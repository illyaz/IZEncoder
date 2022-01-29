namespace IZEncoder.Common.FFMSIndexer
{
    public enum FFMSErrors
    {
        // No error
        SUCCESS = 0,

        // Main types - where the error occurred
        INDEX = 1, // index file handling
        INDEXING, // indexing
        POSTPROCESSING, // video postprocessing (libpostproc)
        SCALING, // image scaling (libswscale)
        DECODING, // audio/video decoding
        SEEKING, // seeking
        PARSER, // file parsing
        TRACK, // track handling
        WAVE_WRITER, // WAVE64 file writer
        CANCELLED, // operation aborted
        RESAMPLING, // audio resampling (libavresample)

        // Subtypes - what caused the error
        UNKNOWN = 20, // unknown error
        UNSUPPORTED, // format or operation is not supported with this binary
        FILE_READ, // cannot read from file
        FILE_WRITE, // cannot write to file
        NO_FILE, // no such file or directory
        VERSION, // wrong version
        ALLOCATION_FAILED, // out of memory
        INVALID_ARGUMENT, // invalid or nonsensical argument
        CODEC, // decoder error
        NOT_AVAILABLE, // requested mode or operation unavailable in this binary
        FILE_MISMATCH, // provided index does not match the file
        USER // problem exists between keyboard and chair
    }
}