float4x4 World;
float4x4 View;
float4x4 Projection;

//color of the light 
float3 Color; 

//position of the camera, for specular light
float3 cameraPosition; 

//this is used to compute the world-position
float4x4 InvertViewProjection; 
//this is used to compute the world-position of shadowmap sample
float4x4 ShadowView;
float4x4 ShadowProjection;
float4x4 ShadowInvertViewProjection;


float3 spotDirection;

float spotLightAngleCosine;

float spotDecayExponent = 1.0f;

//this is the position of the light
float3 lightPosition;

//how far does this light reach
float lightRadius;

//control the brightness of the light
float lightIntensity = 1.0f;

// diffuse color, and specularIntensity in the alpha channel
texture colorMap; 
// normals, and specularPower in the alpha channel
texture normalMap;
// depth
texture depthMap;
// rgb refractive mask & transitive value in alpha channel
texture transMap;
// depth texture
texture shadowMapColor;
texture shadowMapDepth;

//float specularIntensity = 0.0f;

sampler colorSampler = sampler_state
{
    Texture = (colorMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
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
sampler normalSampler = sampler_state
{
    Texture = (normalMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
	MagFilter = LINEAR;
	MinFilter = LINEAR;
	Mipfilter = LINEAR;
};
sampler transSampler = sampler_state
{
    Texture = (transMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
sampler shadowColorSampler = sampler_state
{
	Texture = (shadowMapColor);
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};
sampler shadowDepthSampler = sampler_state
{
	Texture = (shadowMapDepth);
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};



struct VertexShaderInput
{
    float3 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 ScreenPosition : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    //processing geometry coordinates
    float4 worldPosition = mul(float4(input.Position,1), World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.ScreenPosition = output.Position;
    return output;
}

float2 halfPixel;
float2 shadowHalfPixel;
float4 PixelShaderFunction(VertexShaderOutput input, float2 vPos : VPOS) : COLOR0
{
    //obtain screen position
    input.ScreenPosition.xy /= input.ScreenPosition.w;

    float2 texCoord = 0.5f * (float2(input.ScreenPosition.x,-input.ScreenPosition.y) + 1);
    //allign texels to pixels
    texCoord -=halfPixel;
	

    //get normal data from the normalMap
    float4 normalData = tex2D(normalSampler,texCoord);
    //tranform normal back into [-1,1] range
    float3 normal = 2.0f * normalData.xyz - 1.0f;
    //get specular power
    float specularPower = normalData.a * 255;
    //get specular intensity from the colorMap
    float specularIntensity = tex2D(transSampler, texCoord).a;
	float colorAlpha = tex2D(colorSampler, texCoord).a;

    //read depth
    float depthVal = tex2D(depthSampler,texCoord).r;

    //compute screen-space position
    float4 position;
    position.xy = input.ScreenPosition.xy;
    position.z = depthVal;
    position.w = 1.0f;
    //transform to world space
    position = mul(position, InvertViewProjection);
    position /= position.w;

    //surface-to-light vector
    float3 lightVector = lightPosition - position;

    //compute attenuation based on distance - linear attenuation
    float attenuation = saturate(1.0f - length(lightVector)/lightRadius); 

    //normalize light vector
    lightVector = normalize(lightVector); 

	float spotIntensity = 0;

	float SdL = dot(spotDirection, -lightVector);

	if (SdL > spotLightAngleCosine)
	{
		spotIntensity = pow(abs(SdL), spotDecayExponent);
	}

    //compute diffuse light
    float NdL = max(0,dot(normal,lightVector));

	//light transparent objects from both sides
	//if(colorAlpha < 0.99 && NdL == 0)
	//	NdL = max(0,dot(-normal,lightVector)); 

    float3 diffuseLight = NdL * Color.rgb * spotIntensity;

    //reflection vector
    float3 reflectionVector = normalize(reflect(-lightVector, normal));
    //camera-to-surface vector
    float3 directionToCamera = normalize(cameraPosition - position);
    //compute specular light
    float specularLight = specularIntensity * pow( saturate(dot(reflectionVector, directionToCamera)), specularPower);

    //take into account attenuation and lightIntensity.
    return attenuation * spotIntensity * lightIntensity * float4(diffuseLight.rgb,specularLight);
}




float4 PixelShaderFunctionShadow(VertexShaderOutput input, float2 vPos : VPOS) : COLOR0
{
	//obtain screen position
	input.ScreenPosition.xy /= input.ScreenPosition.w;

	float2 texCoord = 0.5f * (float2(input.ScreenPosition.x, -input.ScreenPosition.y) + 1);
	//allign texels to pixels
	texCoord -= halfPixel;


	//get normal data from the normalMap
	float4 normalData = tex2D(normalSampler, texCoord);
	//tranform normal back into [-1,1] range
	float3 normal = 2.0f * normalData.xyz - 1.0f;
	
	float4 transData = tex2D(transSampler, texCoord);

	//get specular intensity from the colorMap
	float specularIntensity = transData.a;
	float colorAlpha = tex2D(colorSampler, texCoord).a;

	//get specular power
	float specularPower = transData.r * 255;

	//read depth
	float depthVal = tex2D(depthSampler, texCoord).r;

	//compute screen-space position
	float4 position;
	position.xy = input.ScreenPosition.xy;
	position.z = depthVal;
	position.w = 1.0f;
	//transform to world space
	position = mul(position, InvertViewProjection);
	position /= position.w;


		//transform to spotlight screen-space
		float4 shadowPosition = mul(position, ShadowInvertViewProjection);
		float2 shadowTexCoord = shadowPosition.xy / shadowPosition.w;
		float shadowDepth = shadowPosition.z / shadowPosition.w;

		shadowTexCoord = 0.5f * (float2(shadowTexCoord.x, -shadowTexCoord.y) + 1);
		shadowTexCoord -= shadowHalfPixel;

		float2 shadowPixSize = -shadowHalfPixel * 2;
		float shadowHLine = shadowTexCoord.y / shadowPixSize.y;
		float shadowVLine = shadowTexCoord.x / shadowPixSize.x;

		float2 uvA = float2(shadowVLine, shadowHLine) * shadowPixSize;
		float2 uvB = float2(shadowVLine, shadowHLine + 1) * shadowPixSize;

		
		float4 shadowColorA = tex2D(shadowColorSampler, uvA);
		float4 shadowColorB = tex2D(shadowColorSampler, uvB);

		float4 temp;
		if (shadowColorB.a < shadowColorA.a)
		{
			temp = shadowColorA;
			shadowColorA = shadowColorB;
			shadowColorB = temp;
			temp.xy = uvA;
			uvA = uvB;
			uvB = temp.xy;
		}

		float shadowCompareDepthA = tex2D(shadowDepthSampler, uvA).r;
		float shadowCompareDepthB = tex2D(shadowDepthSampler, uvB).r;

		float3 lightColor = Color;

			if (shadowDepth > shadowCompareDepthB + 0.0001f)
			{
				lightColor = float3(0, 0, 0);
			}

			if (shadowDepth > shadowCompareDepthA + 0.0001f)
			{
				

					lightColor.r = min(lightColor.r, shadowColorA.r);
					lightColor.g = min(lightColor.g, shadowColorA.g);
					lightColor.b = min(lightColor.b, shadowColorA.b);


				if (shadowColorA.a < 0.99)
				{
					lightColor = lightColor * (1 - shadowColorA.a);
				}
			}
		


	//surface-to-light vector
	float3 lightVector = lightPosition - position;

	//compute attenuation based on distance - linear attenuation
	float attenuation = saturate(1.0f - length(lightVector) / lightRadius);

	//normalize light vector
	lightVector = normalize(lightVector);

	float spotIntensity = 0;

	float SdL = dot(spotDirection, -lightVector);

	if (SdL > spotLightAngleCosine)
	{
		spotIntensity = pow(abs(SdL), spotDecayExponent);
	}

	//compute diffuse light
	float NdL = max(0, dot(normal, lightVector));

	//light transparent objects from both sides
	//if (colorAlpha < 0.99 && NdL == 0)
	//	NdL = max(0, dot(-normal, lightVector));

	float3 diffuseLight = NdL * lightColor.rgb * spotIntensity;

	//reflection vector
	float3 reflectionVector = normalize(reflect(-lightVector, normal));
	//camera-to-surface vector
	float3 directionToCamera = normalize(cameraPosition - position);
	//compute specular light
	float specularLight = specularIntensity * pow(saturate(dot(reflectionVector, directionToCamera)), specularPower);

	//take into account attenuation and lightIntensity.
	return attenuation * spotIntensity * lightIntensity * float4(diffuseLight.rgb, specularLight);
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
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionShadow();
	}
}