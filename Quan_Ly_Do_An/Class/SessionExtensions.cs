namespace Quan_Ly_Do_An.Class
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;

    public static class SessionExtensions
    {
        // Extension method to set an object in Session
        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        // Extension method to get an object from Session
        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }

}
