float4x4 World;
float4x4 View;
float4x4 Projection;


texture Texture;
sampler diffuseSampler = sampler_state
{
    Texture = (Texture);
    MAGFILTER = LINEAR;
    MINFILTER = LINEAR;
    MIPFILTER = LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture SpecularMap;
sampler specularSampler = sampler_state
{
    Texture = (SpecularMap);
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture NormalMap;
sampler normalSampler = sampler_state
{
    Texture = (NormalMap);
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
    float3 Binormal : BINORMAL0;
    float3 Tangent : TANGENT0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float2 Depth : TEXCOORD1;
    float3x3 tangentToWorld : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(float4(input.Position.xyz,1), World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;
    output.Depth.x = output.Position.z;
    output.Depth.y = output.Position.w;

    // calculate tangent space to world space matrix using the world space tangent,
    // binormal, and normal as basis vectors
    output.tangentToWorld[0] = mul(input.Tangent, World);
    output.tangentToWorld[1] = mul(input.Binormal, World);
    output.tangentToWorld[2] = mul(input.Normal, World);

    return output;
}
struct PixelShaderOutput
{
    half4 Color : COLOR0;
    half4 Normal : COLOR1;
    half4 Depth : COLOR2;
	half4 Trans : COLOR3;
};

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input, float2 vPos : VPOS)
{

    PixelShaderOutput output;
    output.Color = tex2D(diffuseSampler, input.TexCoord);
	output.Trans.xyz = floor(output.Color.a);
	

	//interlace transparent objects
	output.Color.rgb = output.Color.rgb / output.Color.a;
	clip(output.Color.a < 0.99 && frac(vPos.y * 0.5) < 0.1 ? -1:1);
	//clip(frac(vPos.y * 0.5) < 0.1 ? -1:1);

    float4 specularAttributes = tex2D(specularSampler, input.TexCoord);
    //specular Intensity
    //output.Color.a = specularAttributes.r;
	//output.Trans.a = specularAttributes.r;
	output.Trans = float4(specularAttributes.a, 0, 0, specularAttributes.r);

    // read the normal from the normal map
    float3 normalFromMap = tex2D(normalSampler, input.TexCoord);
    //tranform to [-1,1]
    normalFromMap = 2.0f * normalFromMap - 1.0f;
    //transform into world space
    normalFromMap = mul(normalFromMap, input.tangentToWorld);
    //normalize the result
    normalFromMap = normalize(normalFromMap);
    //output the normal, in [0,1] space
    output.Normal.rgb = 0.5f * (normalFromMap + 1.0f);

    //specular Power
    output.Normal.a = specularAttributes.a;

    output.Depth = input.Depth.x / input.Depth.y;

	
    return output;
	
}

struct ShadowVertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;

	// TODO: add input channels such as texture
	// coordinates and vertex colors here.
};
struct ShadowVertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float2 Depth : TEXCOORD1;

	// TODO: add vertex shader outputs such as colors and texture
	// coordinates here. These values will automatically be interpolated
	// over the triangle, and provided as input to your pixel shader.
};

ShadowVertexShaderOutput ShadowVertexShaderFunction(ShadowVertexShaderInput input)
{
	ShadowVertexShaderOutput output;

	float4 worldPosition = mul(float4(input.Position.xyz, 1), World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);

	output.TexCoord = input.TexCoord;

	output.Depth.x = output.Position.z;
	output.Depth.y = output.Position.w;

	// TODO: add your vertex shader code here.

	return output;
}
struct ShadowPixelShaderOutput
{
	half4 Color : COLOR0;
	half4 Depth : COLOR1;
};
ShadowPixelShaderOutput ShadowPixelShaderFunction(ShadowVertexShaderOutput input, float2 vPos: VPOS)
{
	// TODO: add your pixel shader code here.
	//float4 output = input.Depth.x / input.Depth.y;
	ShadowPixelShaderOutput output;
	output.Color = float4(0, 0, 0, 0);
	
	
	output.Depth.r = 0;
	output.Depth.g = 1;
	output.Depth.b = 1;
	output.Depth.a = 1;
	
	output.Color = tex2D(diffuseSampler, input.TexCoord);
	output.Color.rgb = output.Color.rgb / output.Color.a;
	clip(output.Color.a < 0.99 && frac(vPos.y * 0.5) < 0.1 ? -1 : 1);
	//clip(output.Color.a < 0.99 1 ? -1 : 1);
	//output.Color.a = 1;
	if (output.Color.a > 0.9)
	{
		output.Color.rgb = float3(0, 0, 0);
	}

	output.Depth.r = input.Depth.x / input.Depth.y;
	return output;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
technique Technique2
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 ShadowVertexShaderFunction();
		PixelShader = compile ps_3_0 ShadowPixelShaderFunction();
	}
}
