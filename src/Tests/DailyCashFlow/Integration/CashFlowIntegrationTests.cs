using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DailyCashFlow.Integration.Clients;
using DailyCashFlow.Integration.Models;
using Xunit;

namespace Tests.DailyCashFlow.IntegrationCashFlow.Tests
{
    public class DailyCashFlowIntegrationTests : IAsyncLifetime
    {
        private readonly DailyCashFlowApiClient _apiClient;
        private readonly string _username;
        private readonly string _password;
        private readonly string _uniqueSession;
        private readonly List<Guid> _createdEntriesInTest;

        public DailyCashFlowIntegrationTests()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true) // Configuração flexível
                .Build();

            // Fallbacks apontando para o seu ambiente local de testes ativos
            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8080"; // 🚀 TESTING: Local test
            //var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:8443";
            _username = configuration["ApiSettings:TestUser"] ?? "test_user";
            _password = configuration["ApiSettings:TestPassword"] ?? "password123";

            // 1. Cria um manipulador HTTP forçando o .NET a ignorar qualquer erro de SSL local do Apigee
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // 2. Injeta o manipulador configurado no HttpClient de integração
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
            _apiClient = new DailyCashFlowApiClient(httpClient);


            _uniqueSession = Guid.NewGuid().ToString();
            _createdEntriesInTest = new List<Guid>();
        }

        // Executado ANTES de cada método de teste [Fact]
        public async Task InitializeAsync()
        {
            // Executa a autenticação real populando o Token com a propriedade maiúscula correta
            await _apiClient.AuthenticateAsync(_username, _password);
        }

        [Fact]
        public async Task IsolatedFlow_ShouldCalculateBalanceCorrectlyAndAutoCleanUp()
        {
            // 1. Arrange
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var credit = new EntryRequest(3756.87m);
            var debit = new EntryRequest(-564.49m); // Enviando o valor negativo para o switch tratar o débito

            // 2. Act
            var resCredit = await _apiClient.CreateEntryAsync(credit);
            var resDebit = await _apiClient.CreateEntryAsync(debit);

            // Armazena com segurança os IDs retornados pela API local para limpeza posterior
            if (resCredit.IsSuccessStatusCode && resCredit.StatusCode != HttpStatusCode.NoContent) {
                try {
                    var body = await resCredit.Content.ReadFromJsonAsync<EntryResponse>();
                    if (body != null && body.Id != Guid.Empty) _createdEntriesInTest.Add(body.Id);
                } catch { /* Ignora se o endpoint local não retornar corpo JSON no POST */ }
            }
            
            if (resDebit.IsSuccessStatusCode && resDebit.StatusCode != HttpStatusCode.NoContent) {
                try {
                    var body = await resDebit.Content.ReadFromJsonAsync<EntryResponse>();
                    if (body != null && body.Id != Guid.Empty) _createdEntriesInTest.Add(body.Id);
                } catch { /* Ignora se o endpoint local não retornar corpo JSON no POST */ }
            }

            // 🚀 ESPERA DE PROCESSAMENTO: Aguarda 5 segundos para a API processar os lançamentos no banco
            await Task.Delay(20000);

            // Realiza a consulta integrada do saldo passando a data e a sessão única gerada no construtor
            var resBalance = await _apiClient.GetBalanceAsync(today, _uniqueSession);

            // 3. Assert
            if (!resBalance.IsSuccessStatusCode)
            {
                // Captura a URL real montada pelo HttpClient e o corpo da resposta do Apigee
                string attemptedUrl = resBalance.RequestMessage?.RequestUri?.ToString() ?? "Desconhecida";
                string errorContent = await resBalance.Content.ReadAsStringAsync();
                
                throw new HttpRequestException($"[ERRO 404 DIAGNÓSTICO] URL enviada: {attemptedUrl} Corpo da Resposta: {errorContent}");
            }

            // 1. Lê a resposta bruta do Controller: '{"balanceValue": 3192,3800000000000000000000000}'
            var rawJson = await resBalance.Content.ReadAsStringAsync();

            // 2. Extrai apenas a parte numérica com a vírgula de dentro do JSON
            var numericPart = rawJson
                .Replace("{\"balanceValue\":", "")
                .Replace("}", "")
                .Trim();

            // 3. Faz o parse decimal usando a cultura brasileira (que entende a vírgula como separador)
            var culturePtBr = new System.Globalization.CultureInfo("pt-BR");
            decimal balanceValue = decimal.Parse(numericPart, culturePtBr);

            // 4. Assert final comparando os valores decimais limpos
            Assert.Equal(3192.38m, balanceValue);
        }

        // Executado DEPOIS de cada método de teste [Fact] (Garante a limpeza da base)
        public async Task DisposeAsync()
        {
            foreach (var id in _createdEntriesInTest)
            {
                try
                {
                    await _apiClient.DeleteEntryAsync(id);
                }
                catch
                {
                    // Evita travar a esteira se um item falhar na remoção
                }
            }
        }
    }
}
