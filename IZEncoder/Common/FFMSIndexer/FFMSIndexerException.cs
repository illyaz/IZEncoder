namespace IZEncoder.Common.FFMSIndexer
{
    using System;

    public class FFMSIndexerException : Exception
    {
        public FFMSErrors ErrorType;
        public FFMSErrors SubType;

        public FFMSIndexerException(string message)
            : base(message)
        {
        }
    }
}