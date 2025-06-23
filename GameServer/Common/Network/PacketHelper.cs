namespace Common.Network
{
    public static class PacketHelper
    {
        /// <summary>
        /// 把 msgId + body 拼成 [4-byte len][4-byte msgId][body] 的格式
        /// </summary>
        public static byte[] CreatePacket(int msgId, byte[] body)
        {
            int total = 8 + body.Length;
            var pkg = new byte[total];
            // 写入总长度
            Buffer.BlockCopy(BitConverter.GetBytes(total), 0, pkg, 0, 4);
            // 写入消息ID
            Buffer.BlockCopy(BitConverter.GetBytes(msgId), 0, pkg, 4, 4);
            // 写入消息体
            Buffer.BlockCopy(body, 0, pkg, 8, body.Length);
            return pkg;
        }
    }
}