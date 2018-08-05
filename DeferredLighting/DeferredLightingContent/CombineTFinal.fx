texture colorMap;
texture lightMap;
texture normalMap;
texture transMap;
texture depthMap;
float4 vScale;
float4x4 Rotation;

sampler colorSampler = sampler_state
{
    Texture = (colorMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};
sampler lightSampler = sampler_state
{
    Texture = (lightMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};
sampler normalSampler = sampler_state
{
    Texture = (normalMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};
sampler transSampler = sampler_state
{
    Texture = (transMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};

sampler depthSampler = sampler_state
{
    Texture = (depthMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
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
	
	/*
	float4 diffuseColor = tex2D(colorSampler, input.TexCoord);
	float4 light = tex2D(lightSampler, input.TexCoord);

    float3 diffuseLight = light.rgb;
    float specularLight = light.a;
    return float4((diffuseColor * diffuseLight + specularLight),1);*/

	//clasic alpha blend start
/*
	float2 uv1 = input.TexCoord - float2(0, -halfPixel.y * 2);
	float2 uv2 = input.TexCoord + float2(0, -halfPixel.y * 2);  //artifact

	float4 colorA = tex2D(colorSampler, input.TexCoord);
	float4 colorB = tex2D(colorSampler, uv1);
	float4 colorC = tex2D(colorSampler, uv2);					  //artifact

	float4 lightA = tex2D(lightSampler, input.TexCoord);
	float4 lightB = tex2D(lightSampler, uv1);

	colorB.a = min(colorB.a, colorC.a);						//artifact

	float a = colorA.a;
	if(colorB.a < colorA.a) a = 1 - colorB.a;

	float4 returnA = float4((colorA.rgb * lightA.rgb + lightA.a), 1);
	float4 returnB = float4((colorB.rgb * lightB.rgb + lightB.a), 1);
	return lerp(returnB, returnA, a);

*/

	//clasic alpha blend end
	
	float2 pixSize = -halfPixel * 2;

	float hLine = (input.TexCoord.y / pixSize.y);
	float vLine = (input.TexCoord.x / pixSize.x);


	float2 uvA = float2(vLine, hLine) * pixSize;
	float2 uvB = float2(vLine, hLine + 1) * pixSize;
	

	
	//float2 uvA = input.TexCoord;
	//float2 uvB = input.TexCoord + float2(0, halfPixel.y * 2);

	//float2 uvA = vPos * pixSize;
	//float2 uvB = (vPos - float2(0,1)) * pixSize;
	
	float4 colorA = tex2D(colorSampler, uvA);
	float4 colorB = tex2D(colorSampler, uvB);

	
	//clip(colorA.a < colorB.a ? -1:1);



//	clip(input.TexCoord == floor(input.TexCoord / pixSize) * pixSize ? -1:1);
	float4 temp;

	float2 frontUV;
	float2 backUV;

	bool asd = colorB.a < colorA.a;
	if(colorB.a < colorA.a)
	{
		temp = colorA;
		colorA = colorB;
		colorB = temp;
		temp.xy = uvA;
		uvA = uvB;
		uvB = temp.xy;
	}

	//get normal data from the normalMap
	float4 normalData = tex2D(normalSampler, uvA);

	//tranform normal into [-1,1] range
	float3 normal = 2.0f * normalData.xyz - 1.0f;

	float4 normal2 = mul(float4(normal, 1), Rotation);
	
	float2 uvC = uvB;
	float depthA = tex2D(depthSampler, uvA);
	float depthB = depthA;

	if (colorA.a < 0.99)
	{
		uvB = uvB + normal2.xy * vScale;
		hLine = (uvB.y / pixSize.y);
		vLine = (uvB.x / pixSize.x);
		uvB = float2(vLine, hLine) * pixSize;
		colorB = tex2D(colorSampler, uvB);
		if (colorB.a < 0.99)
		{
			hLine += 1;
			uvB = float2(vLine, hLine) * pixSize;
		}		
		depthB = tex2D(depthSampler, uvB);
		if (depthA > depthB)
		{
			uvB = uvC;
		}

		colorB = tex2D(colorSampler, uvB);
	}

	float4 lightA = tex2D(lightSampler, uvA);
	float4 lightB = tex2D(lightSampler, uvB);

	float4 frontColor = float4((colorA.rgb * lightA.rgb + lightA.a), 1);
	float4 backColor = float4((colorB.rgb * lightB.rgb + lightB.a), 1);
	
	return lerp(backColor, frontColor, colorA.a);


	//float4 color = lerp(colorB, colorA, a);
	//float4 light = lerp(lightB, lightA, a);

	//return float4((color.rgb * light.rgb + light.a), 1);

}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
