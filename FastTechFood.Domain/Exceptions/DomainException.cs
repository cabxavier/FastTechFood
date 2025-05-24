namespace FastTechFood.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public IEnumerable<string> Errors { get; }

        public DomainException() { }

        public DomainException(string message) : base(message) { }

        public DomainException(string massage, Exception innerException) : base(massage, innerException) { }

        public DomainException(IEnumerable<string> erros) : base("Ocorreram múltiplos erros de validação") { Errors = erros; }
    }
}
