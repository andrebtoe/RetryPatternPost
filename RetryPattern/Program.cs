namespace RetryPattern
{
    public static class Program
    {
        private static readonly PostService _postService = new PostService();

        public static async Task Main()
        {
            var postsToFill = new List<PostDto>();

            // Preencher a lista de posts
            await _postService.FillPostsAsync(postsToFill);

            // Executar o preenchimento de posts novamente para que a idempotência do método seja testada
            await _postService.FillPostsAsync(postsToFill);

            // Salvar os posts no banco de dados
            await _postService.SavePostsInDatabaseAsync(postsToFill);
        }
    }
}
