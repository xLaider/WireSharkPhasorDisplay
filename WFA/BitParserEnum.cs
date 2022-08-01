using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcaPNGTestV2
{
    public enum BitParserEnum
    {
        SHIFT_16_BYTES = 16,
        SYNC_WORD_BYTE_1 = 42,
        SYNC_WORD_BYTE_2 = 43,
        PHASOR_MAGNITUDE = 58,
        PHASOR_MAGNITUDE_END= 61,
        PHASOR_ANGLE = 62,
        PHASOR_ANGLE_END = 65,
        NUM_OF_PHASORS = 82,
        NUM_OF_PHASORS_END = 83,
        NUM_OF_ANALOG_VALUE = 84,
        NUM_OF_ANALOG_VALUE_END = 85,
        PHASOR_NAMES = 88,
        PHASOR_NAMES_END = 103,
        
    }
}
