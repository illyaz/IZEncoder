namespace IZEncoder.Common.AvisynthFilter
{
    using System;

    public enum FilterMtModes
    {
        Invalid = 0,
        NiceFilter = 1,
        MultiInstance = 2,
        Serialized = 3,
        SpecialMt = 4,
        ModeCount = 5
    }

    public static class FilterMtModesHelper
    {
        public static string ToAvisynthPlus(this FilterMtModes mode)
        {
            switch (mode)
            {
                case FilterMtModes.Invalid:
                    return "MT_INVALID";
                case FilterMtModes.NiceFilter:
                    return "MT_NICE_FILTER";
                case FilterMtModes.MultiInstance:
                    return "MT_MULTI_INSTANCE";
                case FilterMtModes.Serialized:
                    return "MT_SERIALIZED";
                case FilterMtModes.SpecialMt:
                    return "MT_SPECIAL_MT";
                case FilterMtModes.ModeCount:
                    return "MT_MODE_COUNT";
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}