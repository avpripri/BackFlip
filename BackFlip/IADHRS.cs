using System.Collections.Generic;

namespace BackFlip
{
    public interface IADHRS
    {
        bool IsOpen { get; set; }
        Dictionary<char, float> RawRead();
    }
}