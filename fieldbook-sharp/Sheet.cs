using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Net.Http;
using Newtonsoft.Json.Serialization;

namespace FieldBook
{
    using Newtonsoft.Json;

    public class SyntheticAttribute : Attribute { }

    public interface IRow
    {
        int Id { get; set; }
    }

    public class Sheet<T> where T : IRow
    {
        string Name { get; }
        HttpClient Client { get; }

        internal Sheet(string name, HttpClient client)
        {
            Name = name;
            Client = client;
        }

        public async Task<List<T>> List(int offset, int limit)
        {
            var url = $"{Name}?offset={offset}&limit={limit}";
            var json = await Client.GetStringAsync(url);
            var reply = JsonConvert.DeserializeObject<ListResponse>(json);
            return reply.Items;
        }

        class ListResponse
        {
            public int Count { get; set; }
            public int Offset { get; set; }
            public List<T> Items { get; set; }
        }

        public async Task<List<T>> List()
        {
            var url = Name;
            var json = await Client.GetStringAsync(url);
            var all = JsonConvert.DeserializeObject<List<T>>(json);
            return all;
        }

        public async Task<T> Get(int id)
        {
            var json = await Client.GetStringAsync($"{Name}/{id}");
            var record = JsonConvert.DeserializeObject<T>(json);
            return record;
        }

        class PropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
        {
            bool seenId = false;

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = base.CreateProperties(type, memberSerialization) as IEnumerable<JsonProperty>;

                // Skip properties marked with Synthetic
                props = props.Where(prop => !prop.AttributeProvider.GetAttributes(typeof(SyntheticAttribute), true).Any());

                // Don't send ids for the root object 
                if (!seenId)
                {
                    props = props.Where(prop => prop.UnderlyingName != "Id");
                    seenId = true;
                }

                foreach (var prop in props)
                {
                    prop.PropertyName = prop.PropertyName.ToLower();
                }

                return props.ToList();
            }
        }

        string ToJson(object record) => JsonConvert.SerializeObject(record, new JsonSerializerSettings
        {
            ContractResolver = new PropertyNamesContractResolver()
        });

        HttpContent ToJsonContent(object record) => new StringContent(ToJson(record), Encoding.UTF8, "application/json");

        public async Task Create(T record)
        {
            var response = await Client.PostAsync(Name, ToJsonContent(record));
            response.EnsureSuccessStatusCode();
        }

        public Task Update(T record, object fields = null) => Update(record.Id, fields ?? record);

        public async Task Update(int id, object fields)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{Name}/{id}")
            {
                Content = ToJsonContent(fields)
            };
            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public Task Delete(T record) => Delete(record.Id);
        public async Task Delete(int id)
        {
            var response = await Client.DeleteAsync($"{Name}/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
