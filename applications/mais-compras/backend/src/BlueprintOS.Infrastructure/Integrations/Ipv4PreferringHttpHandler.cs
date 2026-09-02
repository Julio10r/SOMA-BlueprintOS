using System.Net;
using System.Net.Sockets;

namespace BlueprintOS.Infrastructure.Integrations;

/// <summary>
/// Handler HTTP compartilhado por providers de consulta externa (BrasilAPI/CNPJ, ViaCEP/CEP) que
/// prefere IPv4 e nunca deixa uma tentativa IPv6 pendurada indefinidamente. Causa raiz observada em
/// homologação (2026-09-01): tanto brasilapi.com.br quanto viacep.com.br resolvem primeiro para
/// endereço IPv6 (registro AAAA); em ambientes com IPv6 de saída quebrado/bloqueado (comum em
/// containers/VMs corporativos), <see cref="HttpClient"/> tenta conectar via IPv6 e só falha após o
/// timeout completo do socket, nunca chegando a tentar IPv4 dentro do timeout configurado da
/// aplicação (produzia "A consulta demorou demais" mesmo com a fonte plenamente disponível via
/// IPv4). Este handler resolve o host manualmente e tenta os endereços IPv4 primeiro, com um timeout
/// de conexão curto — caindo para IPv6 apenas se não houver IPv4 disponível.
/// </summary>
public static class Ipv4PreferringHttpHandler
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    public static SocketsHttpHandler Create() => new()
    {
        ConnectCallback = async (context, cancellationToken) =>
        {
            var enderecos = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var ordenados = enderecos
                .OrderBy(endereco => endereco.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ToArray();

            Exception? ultimaFalha = null;
            foreach (var endereco in ordenados)
            {
                var socket = new Socket(endereco.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    using var timeout = new CancellationTokenSource(ConnectTimeout);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                    await socket.ConnectAsync(endereco, context.DnsEndPoint.Port, linked.Token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    ultimaFalha = ex;
                }
            }

            throw new HttpRequestException(
                $"Não foi possível conectar a {context.DnsEndPoint.Host}:{context.DnsEndPoint.Port} (IPv4/IPv6).", ultimaFalha);
        }
    };
}
