namespace PacketUtility
{
    public static class PacketUtils
    {
        public static int GetSizeFromPacket(byte[] Packet)
        {
            return BitConverter.ToInt32(Packet, 0);
        }

        public static byte[] AddPacketHeader(byte[] Original)
        {
            int PacketSize = Original.Length;
            byte[] PacketSizeBuffer = BitConverter.GetBytes(PacketSize);
            byte[] Packet = new byte[PacketSize + PacketSizeBuffer.Length];
            Buffer.BlockCopy(PacketSizeBuffer, 0, Packet, 0, PacketSizeBuffer.Length);
            Buffer.BlockCopy(Original, 0, Packet, PacketSizeBuffer.Length, PacketSize);
            return Packet;
        }
    }
}
