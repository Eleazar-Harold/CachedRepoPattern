namespace CachedRepoPatterns
{
    //Add using of your db context here
    using System;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using System.Web;

    interface ICacheService
    {
        T GetSetItem<T>(string cacheKey, Func<T> getItemCallback) where T : class;
        T GetItemNIDB<T>(string cacheKey, int cacheDuration, Func<T> getItemCallback) where T : class;
        IQueryable<T> GetSetList<T>(Func<IQueryable<T>> getItemsCallback) where T : class;
        IQueryable<T> GetListNIDB<T>(string cacheKey, Func<IQueryable<T>> getItemsCallback) where T : class;
    }
    public class InMemoryCache : ICacheService
    {
        private const int minutes = X; //set cache timeout minutes here
        /// <summary>
        /// [Eleazar Harold]
        /// Cache method to store any kind of object(T - string, int, array etc) 
        /// for a predefined time(cacheDuration).
        /// getItemCallback function defines the type of datatype to be stored
        /// the value to be stored here doesn't have to be an entity db type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="cacheDuration"></param>
        /// <param name="getItemCallback"></param>
        /// <returns></returns>
        public T GetItemNIDB<T>(string cacheKey, int cacheDuration, Func<T> getItemCallback) where T : class
        {
            object CacheLock = new object();
            T item = HttpRuntime.Cache[cacheKey] as T;
            if (item == null)
            {
                lock (CacheLock)
                {
                    item = getItemCallback();
                    HttpRuntime.Cache.Insert(cacheKey, item, null, DateTime.Now.AddMinutes(cacheDuration), TimeSpan.Zero);
                }
            }
            return item;
        }
        /// <summary>
        /// [Eleazar Harold]
        /// Cache method to store single kind of object type(T)
        /// getItemCallback function defines the type of datatype to be stored
        /// the value to be stored here has to be an entity db type.
        /// Cache expires in 3 minutes as per constant defined to dispose result and reloads new result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="getItemCallback"></param>
        /// <returns></returns>
        public T GetSetItem<T>(string cacheKey, Func<T> getItemCallback) where T : class
        {
            object CacheLock = new object();
            cacheKey = ContextExtensions.GetTableName<T>(new Db());
            T item = HttpRuntime.Cache[cacheKey] as T;
            if (item == null)
            {
                lock (CacheLock)
                {
                    item = getItemCallback();
                    HttpRuntime.Cache.Insert(cacheKey, item, null, DateTime.Now.AddMinutes(minutes), TimeSpan.Zero);
                }
            }
            return item;
        }
        /// <summary>
        /// [Eleazar Harold]
        /// Cache method to store List of object type(T)
        /// getItemCallback function defines the type of datatype to be stored
        /// the value to be stored here has to be List of entity db type.
        /// Cache expires in 3 minutes as per constant defined to dispose result and reloads new result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="getItemsCallback"></param>
        /// <returns></returns>
        public IQueryable<T> GetSetList<T>(Func<IQueryable<T>> getItemsCallback) where T : class
        {
            object CacheLock = new object();
            string cacheKey = ContextExtensions.GetTableName<T>(new Db());
            IQueryable<T> item = HttpRuntime.Cache[cacheKey] as IQueryable<T>;
            if (item == null)
            {
                lock (CacheLock)
                {
                    item = getItemsCallback();
                    HttpRuntime.Cache.Insert(cacheKey, item, null, DateTime.Now.AddMinutes(minutes), TimeSpan.Zero);
                    //MemoryCache.Default.Add(cacheKey, item, DateTime.Now.AddMinutes(7));
                }
            }
            return item;
        }
        public IQueryable<T> GetListNIDB<T>(string cacheKey, Func<IQueryable<T>> getItemsCallback) where T : class
        {
            object CacheLock = new object();
            IQueryable<T> item = HttpRuntime.Cache[cacheKey] as IQueryable<T>;
            if (item == null)
            {
                lock (CacheLock)
                {
                    item = getItemsCallback();
                    HttpRuntime.Cache.Insert(cacheKey, item, null, DateTime.Now.AddMinutes(minutes), TimeSpan.Zero);
                    //MemoryCache.Default.Add(cacheKey, item, DateTime.Now.AddMinutes(7));
                }
            }
            return item;
        }
    }
    public static class ContextExtensions
    {
        public static string GetTableName<T>(this Db context) where T : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            return objectContext.GetTableName<T>();
        }
        public static string GetTableName<T>(this ObjectContext context) where T : class
        {
            string sql = context.CreateObjectSet<T>().ToTraceString();
            Regex regex = new Regex(@"FROM\s+(?<table>.+)\s+AS");
            Match match = regex.Match(sql);
            string table = match.Groups["table"].Value;
            return table;
        }
    }
    public class Cached<T> where T : class, new()
    {
        private readonly InMemoryCache memory = new InMemoryCache();
        private readonly UnitOfWork<T> entity = new UnitOfWork<T>();
        public IQueryable<T> GetAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            return memory.GetSetList(() => entity.Repository.Get(filter, orderBy));
        }
    }
}