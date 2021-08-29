namespace NMJToolBox
{
    public static class ParsingExtensions
    {
        public static bool ToBoolean(this float value, bool useAbsolute = false, bool useInverse = false,
            float differentialValue = 0.5f)
        {
            if (useAbsolute) value = System.Math.Abs(value);
            return useInverse ? value < differentialValue : value > differentialValue;
        }
    }
}