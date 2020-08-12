[System.Serializable]
public class UnsupportedInstructionException : System.Exception
{
    public UnsupportedInstructionException() { }
    public UnsupportedInstructionException(string instruction)
        : base(System.String.Format("Unsupported instruction: {0}", instruction)) { }
}