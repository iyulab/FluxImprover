namespace FluxImprover.LMSupply;

using FluxImprover.Services;
using global::LMSupply.Generator.Models;
using LMSupplyChatMessage = global::LMSupply.Generator.Models.ChatMessage;
using FluxChatMessage = FluxImprover.Services.ChatMessage;

/// <summary>
/// FluxImprover와 LMSupply.Generator 간의 옵션 매핑 유틸리티
/// </summary>
internal static class OptionsMapper
{
    /// <summary>
    /// FluxImprover CompletionOptions를 LMSupply GenerationOptions로 변환
    /// </summary>
    public static GenerationOptions ToGenerationOptions(
        CompletionOptions? options,
        LMSupplyGenerationDefaults? defaults = null)
    {
        var result = new GenerationOptions();

        // 기본값 적용
        if (defaults is not null)
        {
            if (defaults.Temperature.HasValue)
                result.Temperature = defaults.Temperature.Value;
            if (defaults.MaxTokens.HasValue)
                result.MaxTokens = defaults.MaxTokens.Value;
            if (defaults.TopP.HasValue)
                result.TopP = defaults.TopP.Value;
            if (defaults.TopK.HasValue)
                result.TopK = defaults.TopK.Value;
            if (defaults.RepetitionPenalty.HasValue)
                result.RepetitionPenalty = defaults.RepetitionPenalty.Value;
        }

        // 요청 옵션 오버라이드
        if (options is not null)
        {
            if (options.Temperature.HasValue)
                result.Temperature = options.Temperature.Value;
            if (options.MaxTokens.HasValue)
                result.MaxTokens = options.MaxTokens.Value;
        }

        return result;
    }

    /// <summary>
    /// FluxImprover ChatMessage를 LMSupply ChatMessage로 변환
    /// </summary>
    public static LMSupplyChatMessage ToLMSupplyChatMessage(FluxChatMessage message)
    {
        return message.Role.ToLowerInvariant() switch
        {
            "system" => LMSupplyChatMessage.System(message.Content),
            "user" => LMSupplyChatMessage.User(message.Content),
            "assistant" => LMSupplyChatMessage.Assistant(message.Content),
            _ => LMSupplyChatMessage.User(message.Content)
        };
    }

    /// <summary>
    /// 대화 메시지 목록 빌드
    /// </summary>
    public static IEnumerable<LMSupplyChatMessage> BuildChatMessages(
        string prompt,
        CompletionOptions? options)
    {
        var messages = new List<LMSupplyChatMessage>();

        // System prompt
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            messages.Add(LMSupplyChatMessage.System(options.SystemPrompt));
        }

        // Previous messages
        if (options?.Messages is not null)
        {
            foreach (var msg in options.Messages)
            {
                messages.Add(ToLMSupplyChatMessage(msg));
            }
        }

        // Current user prompt (with JSON mode instruction if needed)
        var finalPrompt = ApplyJsonModeIfNeeded(prompt, options);
        messages.Add(LMSupplyChatMessage.User(finalPrompt));

        return messages;
    }

    /// <summary>
    /// JSON 모드가 활성화된 경우 프롬프트에 JSON 출력 지시 추가
    /// </summary>
    public static string ApplyJsonModeIfNeeded(string prompt, CompletionOptions? options)
    {
        if (options?.JsonMode != true)
            return prompt;

        var jsonInstruction = options.ResponseSchema is not null
            ? $"\n\nRespond with valid JSON matching this schema:\n{options.ResponseSchema}"
            : "\n\nRespond with valid JSON only.";

        return prompt + jsonInstruction;
    }

    /// <summary>
    /// Chat 컨텍스트가 있는지 확인
    /// </summary>
    public static bool HasChatContext(CompletionOptions? options)
        => !string.IsNullOrEmpty(options?.SystemPrompt) || options?.Messages?.Count > 0;
}
