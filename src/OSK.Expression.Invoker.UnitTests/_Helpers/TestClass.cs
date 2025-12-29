namespace OSK.Expression.Invoker.UnitTests._Helpers
{
    public class TestClass
    {
        public int PropertyA { get; set; }
        public int PropertyB;
        private int _propertyC;

        public void SetC(int c)
        {
            _propertyC = c; 
        }

        public int PropertyC => _propertyC;

        public void MethodA(int a)
        {
            PropertyA = a;
        }

        public int MethodB(int c)
        {
            return c;
        }
    }
}
