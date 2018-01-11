namespace CachedRepoPatterns
{
    using System;
    using System.Linq;
    using System.Data.Entity;
    using System.Linq.Expressions;
    using Model;
    using System.Data.Entity.Validation;
    using System.Collections.Generic;
    public class GenericRepository<T> where T : class
    {
        #region Properties
        internal Db context;
        internal DbSet<T> dbSet;
        #endregion

        #region Constructor
        public BaseGenericRepository(Db _context)
        {
            context = _context;
            dbSet = _context.Set<T>();
        }

        /// <summary>
        /// Get full error [Eleazar Harold]
        /// </summary>
        /// <param name="exc">Exception</param>
        /// <returns>Error</returns>
        protected string GetFullErrorText(DbEntityValidationException exc)
        {
            var msg = string.Empty;
            foreach (var validationErrors in exc.EntityValidationErrors)
                foreach (var error in validationErrors.ValidationErrors)
                    msg += string.Format("Property: {0} Error: {1}", error.PropertyName, error.ErrorMessage) + Environment.NewLine;
            return msg;
        }

        #endregion

        #region Methods

        #region Get and Find Functions
        public virtual IQueryable<T> Get(Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = "", int? take = null, int? skip = null)
        {
            IQueryable<T> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).AsQueryable();
            }
            else if (take != null && skip != null)
            {
                return query.Skip(skip.Value).Take(take.Value);
            }
            else
            {
                return query.AsQueryable();
            }
        }

        /// <summary>
        /// Get entity by identifier [Eleazar Harold]
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <returns>Entity</returns>
        public virtual T GetById(object Id)
        {
            return dbSet.Find(Id);
        }
        public virtual T GetByPredicate(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("Predicate is required.");
            T query = dbSet.FirstOrDefault(predicate);
            return query;
        }
        public virtual IQueryable<T> FindBy(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("Predicate is required.");
            IQueryable<T> query = dbSet.Where(predicate);
            return query;
        }
        protected virtual bool Exists(T entity)
        {
            if (entity == null) throw new ArgumentNullException("Entity should not be null");
            return dbSet.Contains(entity);
        }
        #endregion

        #region Insert Functions
        /// <summary>
        /// Insert entity [Eleazar Harold]
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual void Insert(T entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");
                dbSet.Add(entity);
                Save();
            }
            catch (DbEntityValidationException dbEx)
            {
                throw new AppException(GetFullErrorText(dbEx), dbEx);
            }
        }
        /// <summary>
        /// Insert multiple of type entity [Eleazar Harold]
        /// </summary>
        /// <param name="entities"></param>
        public virtual void Insert(IEnumerable<T> entities)
        {
            try
            {
                var loader = new List<T>();
                if (entities == null)
                    throw new ArgumentNullException("entities");

                const int CommitCount = 200; //set your own best performance number here
                int currentCount = 0;

                while (currentCount < entities.Count())
                {
                    int commitCount = CommitCount;
                    if ((entities.Count() - currentCount) < commitCount)
                        commitCount = entities.Count() - currentCount;

                    for (int i = currentCount; i < (currentCount + commitCount); i++)
                        dbSet.Add(entities.ToList()[i]);
                    Save();

                    for (int i = currentCount; i < (currentCount + commitCount); i++)
                        context.Entry(entities.ToList()[i]).State = EntityState.Detached;

                    currentCount += commitCount;
                }
            }
            catch (DbEntityValidationException dbEx)
            {
                throw new AppException(GetFullErrorText(dbEx), dbEx);
            }
        }
        #endregion

        #region Update Functions
        /// <summary>
        /// Update entity [Eleazar Harold]
        /// </summary>
        /// <param name="entityToUpdate">Entity</param>
        public virtual void Update(T entityToUpdate)
        {
            try
            {
                if (entityToUpdate == null)
                    throw new ArgumentNullException("entity");
                dbSet.Attach(entityToUpdate);
                context.Entry(entityToUpdate).State = EntityState.Modified;
                Save();
            }
            catch (DbEntityValidationException dbEx)
            {
                throw new AppException(GetFullErrorText(dbEx), dbEx);
            }
        }
        /// <summary>
        /// Update Multiple entities [Eleazar Harold]
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual void Update(IEnumerable<T> entitiesToUpdate)
        {
            try
            {
                if (entitiesToUpdate == null)
                    throw new ArgumentNullException("entities");

                Save();
            }
            catch (DbEntityValidationException dbEx)
            {
                throw new AppException(GetFullErrorText(dbEx), dbEx);
            }
        }
        #endregion

        #region Delete Functions
        public virtual void Delete(object Id)
        {
            T entityToDelete = dbSet.Find(Id);
            Delete(entityToDelete);
        }
        public virtual void Delete(T entityToDelete)
        {
            try
            {
                if (entityToDelete == null)
                    throw new ArgumentNullException("entity");

                if (context.Entry(entityToDelete).State == EntityState.Detached)
                {
                    dbSet.Attach(entityToDelete);
                }
                dbSet.Remove(entityToDelete);
                Save();
            }
            catch (DbEntityValidationException dbEx)
            {
                throw new AppException(GetFullErrorText(dbEx), dbEx);
            }
        }
        public virtual void Delete(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("Predicate is required.");
            IQueryable<T> query = dbSet.Where(predicate);
            foreach (var item in query)
            {
                if (context.Entry(item).State == EntityState.Detached)
                {
                    dbSet.Attach(item);
                }
                dbSet.Remove(item);
                Save();
            }
        }

        #endregion

        #region Commit Function 
        protected internal virtual void Save()
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    context.SaveChanges();
                    transaction.Commit();
                }
                catch (DbEntityValidationException dbEx)
                {
                    transaction.Rollback();
                    throw new AppException(GetFullErrorText(dbEx), dbEx);
                }
            }
        }
        #endregion

        #endregion
    }
}