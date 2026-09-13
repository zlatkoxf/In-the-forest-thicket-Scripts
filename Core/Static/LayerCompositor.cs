using System;
using UnityEngine;

public class LayerCompositor
{
    // =========== метод расчета видимости слоев (для графики, логики и т.д.) ===========
    public static void CalculateVisibilityNonAlloc(float[] layers, float[] resultBuffer)
    {
        if (layers == null || resultBuffer == null) return;
        int length = Math.Min(layers.Length, resultBuffer.Length);
        Array.Clear(resultBuffer, 0, resultBuffer.Length);
        float currentTransmittance = 1.0f; 

        for (int i = length - 1; i >= 0; i--)
        {
            float layerAlpha = layers[i] < 0.0f ? 0.0f : (layers[i] > 1.0f ? 1.0f : layers[i]);
            resultBuffer[i] = layerAlpha * currentTransmittance;
            currentTransmittance *= (1.0f - layerAlpha);
            if (currentTransmittance <= 0.0f) break;
        }
    }

    // =========== метод для корректировки линейных весов под аудио-громкость ===========
    public static void ApplyConstantPowerCorrection(float[] visibilityBuffer, float[] volumeBuffer)
    {
        if (visibilityBuffer == null || volumeBuffer == null) return;
        int length = Math.Min(visibilityBuffer.Length, volumeBuffer.Length);
        for (int i = 0; i < length; i++)
        {
            // Извлекаем квадратный корень для сохранения звуковой мощности (Constant Power Crossfade)
            volumeBuffer[i] = MathF.Sqrt(visibilityBuffer[i]);
        }
    }
}
