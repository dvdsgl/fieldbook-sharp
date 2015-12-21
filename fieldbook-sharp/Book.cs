using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FieldBook
{
    class Credentials
    {
        public string User { get; set; }
        public string Key { get; set; }
    }

    public class Book
    {
        HttpClient Client { get; }

        public Book(string bookId)
        {
            Client = CreateClient(bookId);
        }

        public Book(string bookId, string user, string key)
        {
            Client = CreateClient(bookId, new Credentials { User = user, Key = key });
        }

        public Sheet<T> GetSheet<T>(string name = null) where T : IRow => new Sheet<T>(name ?? InferTableName<T>(), Client);

        string InferTableName<T>()
        {
            var name = typeof(T).Name.ToLower();

            if (name.EndsWith("y"))
            {
                return $"{name.Substring(0, name.Length - 1)}ies";
            }
            else
            {
                return $"{name}s";
            }
        }

        HttpClient CreateClient(string bookId, Credentials credentials = null)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri($"https://api.fieldbook.com/v1/{bookId}/")
            };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (credentials != null)
            {
                var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.User}:{credentials.Key}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            }

            return client;
        }
    }
}
