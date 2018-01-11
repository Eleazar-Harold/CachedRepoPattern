namespace CachedRepoPatterns 
{
    using System;
    public class UnitOfWork<T> where T : class
    {
        #region Constructor
        private Db context = new Db(); //instantiate your db context here
        private GenericRepository<T> Repo;
        #endregion
        public GenericRepository<T> Repository
        {
            get
            {
                if (Repo == null)
                {
                    Repo = new GenericRepository<T>(context);
                }
                return Repo;
            }
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}