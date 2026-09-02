import { afterEach, describe, expect, it, vi } from "vitest";
import { listSuppliers, searchSupplierByDocument } from "../services/supplierEnrichmentApi";
import type { Fornecedor } from "../types/linxSupplierContract";

/**
 * Gate de homologação (2026-09-01), item 5: `GET /fornecedores?q=` responde com o contrato
 * paginado (FornecedorPesquisaPaginada, `{ items, totalCount, ... }`) desde o redesenho O1.x da
 * listagem — nunca com um array simples. `searchSupplierByDocument`/`listSuppliers` tratavam a
 * resposta como array e quebravam com "suppliers.find is not a function" (ou equivalente) sempre
 * que o backend respondia no formato real. Estes testes travam a leitura correta de `.items`.
 */

function fornecedor(over: Partial<Fornecedor> = {}): Fornecedor {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    razaoSocial: "ABC Comercio LTDA",
    nomeFantasia: "ABC",
    cnpj_Cpf: "12345678000195",
    tipoPessoa: "PJ",
    status: "Ativo",
    email: "contato@abc.example",
    telefone: "11999999999",
    cidade: "São Paulo",
    estado: "SP",
    ...over
  };
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("searchSupplierByDocument", () => {
  it("le a lista de dentro de items (contrato paginado real) sem lancar 'find is not a function'", async () => {
    const alvo = fornecedor({ cnpj_Cpf: "17797449000125" });
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({ items: [alvo], totalCount: 1, page: 1, pageSize: 20 })
      }))
    );

    const resultado = await searchSupplierByDocument("17.797.449/0001-25");

    expect(resultado?.id).toBe(alvo.id);
  });

  it("retorna null quando nenhum item bate com o documento normalizado", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({ items: [fornecedor({ cnpj_Cpf: "00000000000000" })], totalCount: 1, page: 1, pageSize: 20 })
      }))
    );

    const resultado = await searchSupplierByDocument("17.797.449/0001-25");

    expect(resultado).toBeNull();
  });
});

describe("listSuppliers", () => {
  it("le a lista de dentro de items (contrato paginado real)", async () => {
    const itens = [fornecedor(), fornecedor({ id: "2", razaoSocial: "Beta LTDA" })];
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({ items: itens, totalCount: itens.length, page: 1, pageSize: 20 })
      }))
    );

    const resultado = await listSuppliers();

    expect(resultado).toHaveLength(2);
    expect(resultado[1].razaoSocial).toBe("Beta LTDA");
  });
});
