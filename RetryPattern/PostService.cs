using System.Net.Http.Json;
using Polly;
using Polly.Wrap;

namespace RetryPattern
{
    public class PostService
    {
        public async Task FillPostsAsync(List<PostDto> postsToFill)
        {
            // Obtemos a política para o pattern Retry e Circuit Breaker
            var combinedPolicy = GetPolicyAsync();

            // Obter os posts com a política de Retry e Circuit Breaker e preencher a lista de posts do parametro
            await combinedPolicy.ExecuteAsync(() => GetPostsFromApiAndFillAsync(postsToFill));
        }

        public async Task SavePostsInDatabaseAsync(List<PostDto> posts)
        {
            await Console.Out.WriteLineAsync($"Posts to save: {posts.Count}");
        }

        private static AsyncPolicyWrap GetPolicyAsync()
        {
            // Define a política de Retry (Tentativa de nova execução), com 3 tentativas e um tempo de espera exponencial
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2 * retryAttempt));

            // Define a política de Circuit Breaker (Interrupção de Circuito). Após 2 falhas, quebra o circuito por 30 segundos
            var circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Combina as políticas de Retry e Circuit Breaker
            var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

            return combinedPolicy;
        }

        private static async Task GetPostsFromApiAndFillAsync(List<PostDto> postsToFill)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(
                    "https://jsonplaceholder.typicode.com/posts"
                );

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Error from api: {response.StatusCode}");

                var postsFromApi = await response.Content.ReadFromJsonAsync<List<PostDto>?>();

                if (postsFromApi != null)
                {
                    // Adiciona apenas novos posts, evitando duplicidade
                    foreach (var post in postsFromApi)
                    {
                        if (!postsToFill.Any(p => p.Id == post.Id))
                            postsToFill.Add(post);
                    }
                }
            }
        }
    }
}
