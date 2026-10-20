namespace FluxImprover.Services.Providers;

/// <summary>
/// How <see cref="OpenAICompatibleCompletionService"/> sends <see cref="CompletionOptions.Thinking"/>: the reasoning switch of an
/// OpenAI-compatible server is the server's own dialect, and a server that does not know a field may refuse the request.
/// </summary>
public enum OpenAICompatibleReasoningSwitch
{
    /// <summary>Sends nothing — the model's own default applies. For OpenAI itself, which refuses fields it does not know.</summary>
    None = 0,

    /// <summary>
    /// Sends <c>chat_template_kwargs</c> with <c>enable_thinking</c> (Qwen) and <c>thinking</c> (DeepSeek, IBM Granite) for
    /// <see cref="ThinkingMode.On"/> / <see cref="ThinkingMode.Off"/> — what vLLM, SGLang and llama.cpp servers (GPUStack included)
    /// read for a hybrid-reasoning model. <see cref="ThinkingMode.Auto"/> sends nothing.
    /// </summary>
    ChatTemplateKwargs = 1,
}
