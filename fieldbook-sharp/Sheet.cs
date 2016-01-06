using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Net.Http;
using Newtonsoft.Json.Serialization;

namespace FieldBook
{
    using Newtonsoft.Json;

    public class Sheet<T> where T : IRow
    {
        string Name { get; }
        HttpClient Client { get; }

        internal Sheet(HttpClient client, string name = null)
        {
            Name = name ?? InferredName;
            Client = client;
        }

        string InferredName
        {
            get
            {
                var name = typeof(T).Name.ToLower();
                return name.EndsWith("y")
                    ? $"{name.Substring(0, name.Length - 1)}ies"
                    : $"{name}s";
            }
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

        public Task<T> this[int id] => Get(id);

        public async Task<T> Get(int id)
        {
            var json = await Client.GetStringAsync($"{Name}/{id}");
            var record = JsonConvert.DeserializeObject<T>(json);
            return record;
        }

        // Fielbook doesn't support bools yet so we convert them to string
        class BoolToStringConverted : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var asString = (bool)value ? "true" : "false";
                var token = Newtonsoft.Json.Linq.JToken.FromObject(asString);
                token.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type typ, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override bool CanConvert(Type typ) => typ == typeof(bool);
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

                props = props.Select(prop =>
                {
                    prop.PropertyName = prop.PropertyName.ToLower();
                    return prop;
                });

                return props.ToList();
            }

            protected override JsonConverter ResolveContractConverter(Type objectType)
            {
                if (objectType == typeof(bool))
                    return new BoolToStringConverted();

                return base.ResolveContractConverter(objectType);
            }
        }

        string ToJson(object record) => JsonConvert.SerializeObject(record, new JsonSerializerSettings
        {
            ContractResolver = new PropertyNamesContractResolver()
        });

        HttpContent ToJsonContent(object record) => new StringContent(ToJson(record), Encoding.UTF8, "application/json");

		public async Task<T> Create(T record)
        {
            var response = await Client.PostAsync(Name, ToJsonContent(record));
			response.EnsureSuccessStatusCode();

			var content = response.Content as StreamContent;
			var json = await content.ReadAsStringAsync();
			var created = JsonConvert.DeserializeObject<T>(json);
			return created;
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
