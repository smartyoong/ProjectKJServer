namespace PacketUtility
{
    public static class PacketUtils
    {
        public static int GetSizeFromPacket(byte[] Packet)
        {
            return BitConverter.ToInt32(Packet, 0);
        }
    }
}
