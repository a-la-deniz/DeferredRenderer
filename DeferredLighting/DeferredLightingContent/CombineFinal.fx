texture colorMap;
texture lightMap;

sampler colorSampler = sampler_state
{
    Texture = (colorMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
sampler lightSampler = sampler_state
{
    Texture = (lightMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};


struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

float2 halfPixel;
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = float4(input.Position,1);
    output.TexCoord = input.TexCoord - halfPixel;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input, float2 vPos : VPOS) : COLOR0
{
	

	float4 diffuseColor = tex2D(colorSampler, input.TexCoord);
	float4 light = tex2D(lightSampler, input.TexCoord);

    float3 diffuseLight = light.rgb;
    float specularLight = light.a;
    return float4((diffuseColor * diffuseLight + specularLight),1);
/*
	float2 uv1 = input.TexCoord - float2(0, -halfPixel.y * 2);
	float2 uv2 = input.TexCoord + float2(0, -halfPixel.y * 2);

	float4 colorA = tex2D(colorSampler, input.TexCoord);
	float4 colorB = tex2D(colorSampler, uv1);
	float4 colorC = tex2D(colorSampler, uv2);

	float4 lightA = tex2D(lightSampler, input.TexCoord);
	float4 lightB = tex2D(lightSampler, uv1);

	colorB.a = min(colorB.a, colorC.a);

	float a = colorA.a;
	if(colorB.a < colorA.a) a = 1 - colorB.a;

	float4 color = lerp(colorB, colorA, a);
	float4 light = lerp(lightB, lightA, a);

	return float4((color.rgb * light.rgb + light.a), 1);*/

}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
