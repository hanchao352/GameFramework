namespace Test
{
    public class UpdateEventArgs
    {
        /// <summary>
        /// 事件消息
        /// </summary>
        public string message;
        
        /// <summary>
        /// 当前下载数量
        /// </summary>
        public int currentCount;
        
        /// <summary>
        /// 总下载数量
        /// </summary>
        public int totalCount;
        
        /// <summary>
        /// 当前下载字节数
        /// </summary>
        public long currentBytes;
        
        /// <summary>
        /// 总下载字节数
        /// </summary>
        public long totalBytes;
        
        /// <summary>
        /// 下载进度 (0-1)
        /// </summary>
        public float progress;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string error;
    }
}