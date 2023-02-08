namespace Repository.Interfaces
{
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Saes a list of records
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        Task SaveList(IEnumerable<T> list);
    }
}
