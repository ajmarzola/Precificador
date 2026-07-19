namespace Precificador.Domain.Enums;

public enum ParseState
{
    AwaitingProductHeader = 0,
    ReadingBom = 1,
    ReadingFinancials = 2
}
