using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ParticleEditor
{
    /*
     * Edge Cases:
     * If the actual minimum value is 0, MinValue is null, keep null even if minimum value is increased
     * If MinValueVec and MaxValueVec are equal, there are only 3 values instead of 6, enforce keeping them equal
     * If only MinValueVec is defined then there is no MinValue or MaxValue
     * If both vectors are null just don't allow editing
     */ 
    public class ColorOverLife
    {
        public int offset;
        public float MinValue;
        public float MaxValue;
        public RGB MinValueVec;
        public RGB MaxValueVec;
        public List<float> Values;
        public bool NoVectors => MinValueVec == null && MaxValueVec == null;
        public bool EnforceEqualVectors => NoVectors || MinValueVec.Equals(MaxValueVec);
        public bool KeepMinValueZero => MinValue == 0;
        public bool NoMinOrMaxValue => MaxValueVec == null;
    }
    public class RGB
    {
        public float R;
        public float G;
        public float B;
        public float Min => Math.Min(Math.Min(R, G), B);
        public float Max => Math.Max(Math.Max(R, G), B);
        public bool Equals(RGB other)
        {
            if (other == null)
                return false;
            return (this.R == other.R && this.G == other.G && this.B == other.B);
        }
    }
}
