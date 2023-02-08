namespace BusinessLogic.Interfaces
{
    public interface ISftpBusiness
    {
        /// <summary>
        /// Monitor folder for files
        /// </summary>
        /// <returns>The list of files</returns>
        Task<List<string>> MonitorFolder();

        /// <summary>
        /// Monitor folder for files
        /// </summary>
        /// <returns>The list of files</returns>
        Task<List<string>> UploadFilesToServer();
    }
}
