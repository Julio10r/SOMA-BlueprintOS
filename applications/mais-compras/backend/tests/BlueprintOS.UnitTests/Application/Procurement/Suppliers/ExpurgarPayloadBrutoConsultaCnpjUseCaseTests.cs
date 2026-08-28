using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class ExpurgarPayloadBrutoConsultaCnpjUseCaseTests
{
    [Fact]
    public async Task Execute_Should_Return_Quantity_Reported_By_Repository()
    {
        var repository = new FakeRepository(quantidadeAExpurgar: 3);
        var useCase = new ExpurgarPayloadBrutoConsultaCnpjUseCase(repository,
            NullLogger<ExpurgarPayloadBrutoConsultaCnpjUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync();

        Assert.Equal(3, resultado);
    }

    [Fact]
    public async Task Execute_Should_Pass_A_Utc_Reference_To_The_Repository()
    {
        var repository = new FakeRepository(quantidadeAExpurgar: 0);
        var useCase = new ExpurgarPayloadBrutoConsultaCnpjUseCase(repository,
            NullLogger<ExpurgarPayloadBrutoConsultaCnpjUseCase>.Instance);

        await useCase.ExecuteAsync();

        Assert.NotNull(repository.ReferenciaRecebida);
        Assert.Equal(TimeSpan.Zero, repository.ReferenciaRecebida!.Value.Offset);
    }

    [Fact]
    public async Task Execute_Should_Be_Safe_To_Run_Twice_In_A_Row()
    {
        var repository = new FakeRepository(quantidadeAExpurgar: 1);
        var useCase = new ExpurgarPayloadBrutoConsultaCnpjUseCase(repository,
            NullLogger<ExpurgarPayloadBrutoConsultaCnpjUseCase>.Instance);

        var primeira = await useCase.ExecuteAsync();
        var segunda = await useCase.ExecuteAsync();

        Assert.Equal(1, primeira);
        Assert.Equal(1, segunda);
        Assert.Equal(2, repository.ChamadasRecebidas);
    }

    private sealed class FakeRepository(int quantidadeAExpurgar) : IFornecedorCnpjConsultaHistoricoRepository
    {
        public DateTimeOffset? ReferenciaRecebida { get; private set; }
        public int ChamadasRecebidas { get; private set; }

        public Task AdicionarAsync(FornecedorCnpjConsultaHistorico consulta, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by this test.");

        public Task<int> ExpurgarPayloadBrutoExpiradoAsync(DateTimeOffset referenciaUtc, CancellationToken cancellationToken = default)
        {
            ReferenciaRecebida = referenciaUtc;
            ChamadasRecebidas++;
            return Task.FromResult(quantidadeAExpurgar);
        }
    }
}
