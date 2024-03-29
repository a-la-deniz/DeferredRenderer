struct VertexShaderInput
{
    float3 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = float4(input.Position,1);
    return output;
}
struct PixelShaderOutput
{
    float4 Color : COLOR0;
    float4 Normal : COLOR1;
    float4 Depth : COLOR2;
	float4 Trans: COLOR3;
};

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
    PixelShaderOutput output;
    //black color
    output.Color = 0.0f;
    output.Color.a = 1.0f;
    //when transforming 0.5f into [-1,1], we will get 0.0f
    output.Normal.rgb = 0.5f;
    //no specular power
    output.Normal.a = 0.0f;
    //max depth
    output.Depth = 1.0f;
	//clear refractive mask to white
	output.Trans.rgb = 1.0f;
	//clear alpha channel
	output.Trans.a = 0.0f;
    return output;
}

struct ShadowPixelShaderOutput
{
	half4 Color : COLOR0;
	half4 Depth : COLOR1;
};

ShadowPixelShaderOutput PixelShaderFunction2(VertexShaderOutput input)
{
	ShadowPixelShaderOutput output;

	output.Color = float4(0, 0, 0, 0);
	output.Depth = float4(0, 0, 0, 0);

	output.Depth.r = 1.0f;

	return output;
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}

technique Technique2
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction2();
	}
}