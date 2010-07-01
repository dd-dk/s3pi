using System;
namespace CASPartResource
{
    [Flags]
    public enum FacialRegionFlags : uint
    {
        None = 0x000,
        Eyes = 0x001,
        Nose = 0x002,
        Mouth = 0x004,
        TranslateMouth = 0x008,
        Ears = 0x010,
        TranslateEyes = 0x020,
        Face = 0x040,
        Head = 0x080,
        Brow = 0x100,
        Jaw = 0x200,
        Body = 0x400,
        Eyelashes = 0x800,
    }

    [Flags]
    public enum AgeGenderFlags : uint
    {
        None = 0x00000000,
        Baby = 0x00000001,
        Toddler = 0x00000002,
        Child = 0x00000004,
        Teen = 0x00000008,
        YoungAdult = 0x00000010,
        Adult = 0x00000020,
        Elder = 0x00000040,
        //AgeMask=0x0000007F,

        Male = 0x00001000,
        Female = 0x00002000,
        //GenderMask=0x00003000,
    }

    [Flags]
    public enum DataTypeFlags : uint
    {
        Hair = 0x00000001,
        Scalp = 0x00000002,
        FaceOverlay = 0x00000004,
        Body = 0x00000008,
        Accessory = 0x00000010,
    }

    [Flags]
    public enum ClothingCategoryFlags : uint
    {
        None = 0x00000000,
        Naked = 0x00000001,
        Everyday = 0x00000002,
        Formalwear = 0x00000004,
        Sleepwear = 0x00000008,
        Swimwear = 0x00000010,
        Athletic = 0x00000020,
        Singed = 0x00000040,
        Career = 0x00000100,
        //CategoryMask = 0x000007FF,
        //All = 0x0000FFFF,

        ValidForRandom = 0x00200000,
        IsHat = 0x00400000,
        ValidForMaternity = 0x00800000,
        IsHiddenInCAS = 0x01000000,
    }
}