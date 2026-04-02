Shader "Cossacks2Bridge/TerrainArrayBlendPreview"
{
    Properties
    {
        _GroundArray("Ground Array", 2DArray) = "" {}
        _FactureArray("Facture Array", 2DArray) = "" {}
        _ParityPreviewTex("Parity Preview Tex", 2D) = "black" {}
        _UseParityPreview("Use Parity Preview", Float) = 0
        _Control0("Control0", 2D) = "black" {}
        _Control1("Control1", 2D) = "black" {}
        _Control2("Control2", 2D) = "black" {}
        _Control3("Control3", 2D) = "black" {}
        _Control4("Control4", 2D) = "black" {}
        _FactureMeta0("Facture Meta0", 2D) = "black" {}
        _FactureMeta1("Facture Meta1", 2D) = "black" {}
        _GroundTiling("Ground Tiling", Float) = 256.0
        _SecondaryTiling("Secondary Tiling", Float) = 224.0
        _FactureTiling("Facture Tiling", Float) = 192.0
        _FactureStrength("Facture Strength", Range(0,1)) = 0.08
        _SecondWeightScale("Second Weight Scale", Float) = 1.0
        _FactureWeightScale("Facture Weight Scale", Float) = 5.0
        _FactureEnabled("Facture Enabled", Float) = 1
        _DebugMode("Debug Mode", Float) = 4
        _AmbientStrength("Ambient Strength", Range(0,2)) = 0.52
        _DiffuseStrength("Diffuse Strength", Range(0,2)) = 0.78
        _Brightness("Brightness", Range(0,2)) = 1.08
        _FakeLightDir("Fake Light Dir", Vector) = (0.45,0.85,0.35,0)
        _SlopeBlendPower("Slope Blend Power", Range(1,8)) = 5.25
        _SlopeSecondAtten("Slope Second Atten", Range(0,1)) = 0.42
        _SlopeFactureAtten("Slope Facture Atten", Range(0,1)) = 0.72
        _HeightInfluence("Height Influence", Range(0,1)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "TerrainPreview"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma require 2darray
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_GroundArray);
            SAMPLER(sampler_GroundArray);

            TEXTURE2D_ARRAY(_FactureArray);
            SAMPLER(sampler_FactureArray);

            TEXTURE2D(_ParityPreviewTex);
            SAMPLER(sampler_ParityPreviewTex);

            TEXTURE2D(_Control0);
            SAMPLER(sampler_Control0);

            TEXTURE2D(_Control1);
            SAMPLER(sampler_Control1);

            TEXTURE2D(_Control2);
            SAMPLER(sampler_Control2);

            TEXTURE2D(_Control3);
            SAMPLER(sampler_Control3);

            TEXTURE2D(_Control4);
            SAMPLER(sampler_Control4);

            TEXTURE2D(_FactureMeta0);
            SAMPLER(sampler_FactureMeta0);

            TEXTURE2D(_FactureMeta1);
            SAMPLER(sampler_FactureMeta1);

            float _GroundTiling;
            float _SecondaryTiling;
            float _FactureTiling;
            float _FactureStrength;
            float _SecondWeightScale;
            float _FactureWeightScale;
            float _FactureEnabled;
            float _DebugMode;
            float _UseParityPreview;
            float _AmbientStrength;
            float _DiffuseStrength;
            float _Brightness;
            float4 _FakeLightDir;
            float _SlopeBlendPower;
            float _SlopeSecondAtten;
            float _SlopeFactureAtten;
            float _HeightInfluence;
            float4 _Control0_TexelSize;
            float4 _FactureMeta0_TexelSize;
            float4 _FactureMeta1_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 localPos    : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.localPos = v.positionOS.xyz;
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            float3 ComputeTriWeights(float3 n)
            {
                float3 an = abs(normalize(n));
                an = pow(an, max(_SlopeBlendPower, 1.0));
                float s = max(an.x + an.y + an.z, 1e-5);
                float3 tri = an / s;
                float topBias = saturate((an.y - max(an.x, an.z)) * 1.5 + 0.5);
                tri.y = lerp(tri.y, 1.0, topBias * 0.12);
                float rs = max(tri.x + tri.y + tri.z, 1e-5);
                return tri / rs;
            }

            float2 ComputeOriginalGroundUV(float xIdx, float yIdx, float rawHeight, float rawOption, float use, float opt)
            {
                float optFrac = fmod(opt, 16.0) / 16.0;
                float u = (xIdx * 32.0) / 256.0 + optFrac;
                float v;
                if (use < 0.5)
                {
                    v = ((yIdx * 32.0) + rawHeight) / 256.0 + optFrac;
                }
                else
                {
                    u -= rawOption * 0.02;
                    v = (-rawHeight) / 180.0 + optFrac;
                }
                return float2(u, v);
            }

            float2 GetControlDims()
            {
                return float2(_Control0_TexelSize.z, _Control0_TexelSize.w);
            }

            void LoadCorner(float2 coord, float2 dims,
                out float baseLayer, out float secLayer, out float factLayer,
                out float secWeight, out float factWeight, out float crossType, out float dominantLayer,
                out float baseUse, out float secUse, out float rawHeight, out float rawOption,
                out float baseOpt, out float secOpt, out float factOpt, out float factMaxWeight, out float factEdgeV)
            {
                float2 uv = (coord + 0.5) / dims;
                float4 c0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, uv);
                float4 c1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control1, uv);
                float4 c2 = SAMPLE_TEXTURE2D(_Control2, sampler_Control2, uv);
                float4 c3 = SAMPLE_TEXTURE2D(_Control3, sampler_Control3, uv);
                float4 c4 = SAMPLE_TEXTURE2D(_Control4, sampler_Control4, uv);

                baseLayer = round(c0.r * 255.0);
                secLayer = round(c0.g * 255.0);
                factLayer = round(c0.b * 255.0);
                secWeight = saturate(c0.a);
                factWeight = saturate(c1.r);
                crossType = round(c1.g * 255.0);
                dominantLayer = round(c1.a * 255.0);
                baseUse = c2.r;
                secUse = c2.g;
                rawHeight = c2.b;
                rawOption = c2.a;
                baseOpt = round(c3.r * 255.0);
                secOpt = round(c3.g * 255.0);
                factOpt = round(c4.r * 255.0);
                factMaxWeight = c4.g;
                factEdgeV = c4.b;
            }

            float3 ComputeBarycentric(float2 p, float2 a, float2 b, float2 c)
            {
                float2 v0 = b - a;
                float2 v1 = c - a;
                float2 v2 = p - a;
                float den = v0.x * v1.y - v1.x * v0.y;
                if (abs(den) < 1e-6)
                    return float3(1.0, 0.0, 0.0);
                float inv = 1.0 / den;
                float v = (v2.x * v1.y - v1.x * v2.y) * inv;
                float w = (v0.x * v2.y - v2.x * v0.y) * inv;
                float u = 1.0 - v - w;
                return float3(u, v, w);
            }

            void SelectCellTriangle(float2 cell, float2 f,
                float b00, float b10, float b01, float b11,
                float s00, float s10, float s01, float s11,
                float fl00, float fl10, float fl01, float fl11,
                float sw00, float sw10, float sw01, float sw11,
                float fw00, float fw10, float fw01, float fw11,
                float cr00, float cr10, float cr01, float cr11,
                float bu00, float bu10, float bu01, float bu11,
                float su00, float su10, float su01, float su11,
                float h00, float h10, float h01, float h11,
                float o00, float o10, float o01, float o11,
                float bo00, float bo10, float bo01, float bo11,
                float so00, float so10, float so01, float so11,
                float fo00, float fo10, float fo01, float fo11,
                float fwm00, float fwm10, float fwm01, float fwm11,
                float fe00, float fe10, float fe01, float fe11,
                out float3 bary,
                out float3 triBaseLayers,
                out float3 triSecLayers,
                out float3 triFactLayers,
                out float3 triSecWeights,
                out float3 triFactWeights,
                out float3 triCross,
                out float3 triBaseUse,
                out float3 triSecUse,
                out float3 triHeights,
                out float3 triOptions,
                out float3 triBaseOpts,
                out float3 triSecOpts,
                out float3 triFactOpts,
                out float3 triFactMaxW,
                out float3 triFactEdgeV,
                out float3 triXIdx,
                out float3 triYIdx)
            {
                bool odd = fmod(cell.x, 2.0) > 0.5;
                if (odd)
                {
                    if (f.x + f.y <= 1.0)
                    {
                        bary = ComputeBarycentric(f, float2(0.0, 0.0), float2(1.0, 0.0), float2(0.0, 1.0));
                        triBaseLayers = float3(b00, b10, b01);
                        triSecLayers = float3(s00, s10, s01);
                        triFactLayers = float3(fl00, fl10, fl01);
                        triSecWeights = float3(sw00, sw10, sw01);
                        triFactWeights = float3(fw00, fw10, fw01);
                        triCross = float3(cr00, cr10, cr01);
                        triBaseUse = float3(bu00, bu10, bu01);
                        triSecUse = float3(su00, su10, su01);
                        triHeights = float3(h00, h10, h01);
                        triOptions = float3(o00, o10, o01);
                        triBaseOpts = float3(bo00, bo10, bo01);
                        triSecOpts = float3(so00, so10, so01);
                        triFactOpts = float3(fo00, fo10, fo01);
                        triFactMaxW = float3(fwm00, fwm10, fwm01);
                        triFactEdgeV = float3(fe00, fe10, fe01);
                        triXIdx = float3(cell.x, cell.x + 1.0, cell.x);
                        triYIdx = float3(cell.y, cell.y, cell.y + 1.0);
                    }
                    else
                    {
                        bary = ComputeBarycentric(f, float2(0.0, 1.0), float2(1.0, 0.0), float2(1.0, 1.0));
                        triBaseLayers = float3(b01, b10, b11);
                        triSecLayers = float3(s01, s10, s11);
                        triFactLayers = float3(fl01, fl10, fl11);
                        triSecWeights = float3(sw01, sw10, sw11);
                        triFactWeights = float3(fw01, fw10, fw11);
                        triCross = float3(cr01, cr10, cr11);
                        triBaseUse = float3(bu01, bu10, bu11);
                        triSecUse = float3(su01, su10, su11);
                        triHeights = float3(h01, h10, h11);
                        triOptions = float3(o01, o10, o11);
                        triBaseOpts = float3(bo01, bo10, bo11);
                        triSecOpts = float3(so01, so10, so11);
                        triFactOpts = float3(fo01, fo10, fo11);
                        triFactMaxW = float3(fwm01, fwm10, fwm11);
                        triFactEdgeV = float3(fe01, fe10, fe11);
                        triXIdx = float3(cell.x, cell.x + 1.0, cell.x + 1.0);
                        triYIdx = float3(cell.y + 1.0, cell.y, cell.y + 1.0);
                    }
                }
                else
                {
                    if (f.y <= f.x)
                    {
                        bary = ComputeBarycentric(f, float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0));
                        triBaseLayers = float3(b00, b10, b11);
                        triSecLayers = float3(s00, s10, s11);
                        triFactLayers = float3(fl00, fl10, fl11);
                        triSecWeights = float3(sw00, sw10, sw11);
                        triFactWeights = float3(fw00, fw10, fw11);
                        triCross = float3(cr00, cr10, cr11);
                        triBaseUse = float3(bu00, bu10, bu11);
                        triSecUse = float3(su00, su10, su11);
                        triHeights = float3(h00, h10, h11);
                        triOptions = float3(o00, o10, o11);
                        triBaseOpts = float3(bo00, bo10, bo11);
                        triSecOpts = float3(so00, so10, so11);
                        triFactOpts = float3(fo00, fo10, fo11);
                        triFactMaxW = float3(fwm00, fwm10, fwm11);
                        triFactEdgeV = float3(fe00, fe10, fe11);
                        triXIdx = float3(cell.x, cell.x + 1.0, cell.x + 1.0);
                        triYIdx = float3(cell.y, cell.y, cell.y + 1.0);
                    }
                    else
                    {
                        bary = ComputeBarycentric(f, float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0));
                        triBaseLayers = float3(b00, b11, b01);
                        triSecLayers = float3(s00, s11, s01);
                        triFactLayers = float3(fl00, fl11, fl01);
                        triSecWeights = float3(sw00, sw11, sw01);
                        triFactWeights = float3(fw00, fw11, fw01);
                        triCross = float3(cr00, cr11, cr01);
                        triBaseUse = float3(bu00, bu11, bu01);
                        triSecUse = float3(su00, su11, su01);
                        triHeights = float3(h00, h11, h01);
                        triOptions = float3(o00, o11, o01);
                        triBaseOpts = float3(bo00, bo11, bo01);
                        triSecOpts = float3(so00, so11, so01);
                        triFactOpts = float3(fo00, fo11, fo01);
                        triFactMaxW = float3(fwm00, fwm11, fwm01);
                        triFactEdgeV = float3(fe00, fe11, fe01);
                        triXIdx = float3(cell.x, cell.x + 1.0, cell.x);
                        triYIdx = float3(cell.y, cell.y + 1.0, cell.y + 1.0);
                    }
                }
                bary = saturate(bary);
                float s = max(bary.x + bary.y + bary.z, 1e-5);
                bary /= s;
            }

            float2 InterpolateGroundUVTri(float3 bary, float3 xIdx, float3 yIdx, float use,
                float3 triHeights, float3 triOptions, float3 triOpts, float tiling)
            {
                float2 uv0 = ComputeOriginalGroundUV(xIdx.x, yIdx.x, triHeights.x, triOptions.x, use, triOpts.x);
                float2 uv1 = ComputeOriginalGroundUV(xIdx.y, yIdx.y, triHeights.y, triOptions.y, use, triOpts.y);
                float2 uv2 = ComputeOriginalGroundUV(xIdx.z, yIdx.z, triHeights.z, triOptions.z, use, triOpts.z);
                float2 uv = uv0 * bary.x + uv1 * bary.y + uv2 * bary.z;
                float scale = 256.0 / max(tiling, 1.0);
                return frac(uv * scale);
            }

            float ResolveLayerUse(float candidateLayer, float3 triLayers, float3 triUses)
            {
                if (abs(triLayers.x - candidateLayer) < 0.5) return triUses.x;
                if (abs(triLayers.y - candidateLayer) < 0.5) return triUses.y;
                return triUses.z;
            }

            half4 SampleGroundTriPass(float layer, float use, float3 bary, float3 triXIdx, float3 triYIdx,
                float3 triHeights, float3 triOptions, float3 triOpts, float tiling)
            {
                float2 uv = InterpolateGroundUVTri(bary, triXIdx, triYIdx, use, triHeights, triOptions, triOpts, tiling);
                return SAMPLE_TEXTURE2D_ARRAY(_GroundArray, sampler_GroundArray, uv, layer);
            }

            void AddGroundPassTri(inout half3 accum, inout float total,
                float candidateLayer,
                float3 bary,
                float3 triLayers,
                float3 triUses,
                float3 triCornerWeights,
                float3 triXIdx,
                float3 triYIdx,
                float3 triHeights,
                float3 triOptions,
                float3 triOpts,
                float tiling)
            {
                float3 match = 1.0 - step(0.5, abs(triLayers - candidateLayer));
                float alpha = dot(match * triCornerWeights, bary);
                if (alpha <= 1e-5)
                    return;
                float use = ResolveLayerUse(candidateLayer, triLayers, triUses);
                half4 texCol = SampleGroundTriPass(candidateLayer, use, bary, triXIdx, triYIdx, triHeights, triOptions, triOpts, tiling);
                accum += texCol.rgb * alpha;
                total += alpha;
            }

            float SelectDominantLayer3(float3 layers, float3 weights)
            {
                float bestW = weights.x;
                float bestL = layers.x;
                if (weights.y > bestW) { bestW = weights.y; bestL = layers.y; }
                if (weights.z > bestW) { bestW = weights.z; bestL = layers.z; }
                return bestL;
            }

            half4 SampleFactureMapped(float layer, float3 p, float3 triW, float3 n, float tiling, float factOpt, float factEdgeV)
            {
                float width = max(_FactureMeta0_TexelSize.z, 1.0);
                float uMeta = (floor(layer + 0.5) + 0.5) / width;
                half4 meta0 = SAMPLE_TEXTURE2D(_FactureMeta0, sampler_FactureMeta0, float2(uMeta, 0.5));
                half4 meta1 = SAMPLE_TEXTURE2D(_FactureMeta1, sampler_FactureMeta1, float2(uMeta, 0.5));

                float mapping = floor(meta0.r * 255.0 + 0.5);
                float uScale = max(abs(meta0.g), 0.0001);
                float vScale = max(abs(meta0.b), 0.0001);
                float uShift = meta0.a;
                float vShift = meta1.r;
                float optFrac = fmod(factOpt, 16.0) / 16.0;

                float2 uv;
                if (mapping < 0.5)
                {
                    float tx = p.x / max(tiling, 1.0);
                    float ty = p.z / max(tiling * 2.0, 1.0);
                    uv = float2(tx, ty);
                }
                else if (mapping < 1.5)
                {
                    float useX = step(abs(n.z), abs(n.x));
                    float u = lerp(p.z, p.x, useX) / max(tiling, 1.0);
                    float v = (-p.y) / 180.0;
                    uv = float2(u, v) * 1.5;
                }
                else
                {
                    float useX = step(abs(n.z), abs(n.x));
                    float u = lerp(p.z + p.y, p.x + p.y, useX) / (max(tiling, 1.0) * 1.4142);
                    float edge = saturate(factEdgeV > 0.0 ? factEdgeV : ((1.0 - abs(n.y)) * 2.0));
                    uv = float2(u * 1.5, edge * 1.5);
                }

                uv = frac(float2((uv.x + uShift + optFrac) * uScale, (uv.y + vShift + optFrac) * vScale));
                return SAMPLE_TEXTURE2D_ARRAY(_FactureArray, sampler_FactureArray, uv, layer);
            }

            half3 FalseColorFromIndex(float idx)
            {
                float3 s = sin(float3(idx * 12.9898 + 0.15, idx * 78.233 + 1.7, idx * 37.719 + 2.9));
                float3 h = frac(s * 43758.5453);
                return lerp(half3(0.15, 0.15, 0.15), (half3)h, 0.85);
            }

            float ComputeCrossingMask(float crossType, float2 localCellUv)
            {
                float2 f = frac(localCellUv);
                float d;
                if (crossType < 0.5)
                {
                    d = f.x + f.y;
                }
                else if (crossType < 1.5)
                {
                    d = (1.0 - f.x) + f.y;
                }
                else if (crossType < 2.5)
                {
                    d = f.x + (1.0 - f.y);
                }
                else
                {
                    d = (1.0 - f.x) + (1.0 - f.y);
                }

                float band = saturate(d * 0.5);
                return smoothstep(0.18, 0.82, band);
            }

            float ScaleStage2Weight(float weight01)
            {
                float w = saturate(weight01) * 255.0;
                w = saturate((w * 6.0 / 5.0) / 255.0);
                return w;
            }

            half4 frag(Varyings i) : SV_Target
            {
                if (_UseParityPreview > 0.5)
                {
                    half4 parity = SAMPLE_TEXTURE2D(_ParityPreviewTex, sampler_ParityPreviewTex, saturate(i.uv));
                    parity.a = 1.0;
                    return parity;
                }

                float2 dims = GetControlDims();
                float2 grid = i.uv * max(dims - 1.0, float2(1.0, 1.0));
                float2 cell = floor(grid);
                float2 f = frac(grid);
                float2 maxCell = max(dims - 2.0, float2(0.0, 0.0));
                cell = clamp(cell, float2(0.0, 0.0), maxCell);

                float b00, s00, fl00, sw00, fw00, cr00, dl00, bu00, su00, h00, o00, bo00, so00, fo00, fwm00, fe00;
                float b10, s10, fl10, sw10, fw10, cr10, dl10, bu10, su10, h10, o10, bo10, so10, fo10, fwm10, fe10;
                float b01, s01, fl01, sw01, fw01, cr01, dl01, bu01, su01, h01, o01, bo01, so01, fo01, fwm01, fe01;
                float b11, s11, fl11, sw11, fw11, cr11, dl11, bu11, su11, h11, o11, bo11, so11, fo11, fwm11, fe11;

                LoadCorner(cell + float2(0,0), dims, b00,s00,fl00,sw00,fw00,cr00,dl00,bu00,su00,h00,o00,bo00,so00,fo00,fwm00,fe00);
                LoadCorner(cell + float2(1,0), dims, b10,s10,fl10,sw10,fw10,cr10,dl10,bu10,su10,h10,o10,bo10,so10,fo10,fwm10,fe10);
                LoadCorner(cell + float2(0,1), dims, b01,s01,fl01,sw01,fw01,cr01,dl01,bu01,su01,h01,o01,bo01,so01,fo01,fwm01,fe01);
                LoadCorner(cell + float2(1,1), dims, b11,s11,fl11,sw11,fw11,cr11,dl11,bu11,su11,h11,o11,bo11,so11,fo11,fwm11,fe11);

                float3 bary;
                float3 triBaseLayers, triSecLayers, triFactLayers;
                float3 triSecWeights, triFactWeights, triCross;
                float3 triBaseUse, triSecUse;
                float3 triHeights, triOptions, triBaseOpts, triSecOpts;
                float3 triFactOpts, triFactMaxW, triFactEdgeV;
                float3 triXIdx, triYIdx;

                SelectCellTriangle(cell, f,
                    b00, b10, b01, b11,
                    s00, s10, s01, s11,
                    fl00, fl10, fl01, fl11,
                    sw00, sw10, sw01, sw11,
                    fw00, fw10, fw01, fw11,
                    cr00, cr10, cr01, cr11,
                    bu00, bu10, bu01, bu11,
                    su00, su10, su01, su11,
                    h00, h10, h01, h11,
                    o00, o10, o01, o11,
                    bo00, bo10, bo01, bo11,
                    so00, so10, so01, so11,
                    fo00, fo10, fo01, fo11,
                    fwm00, fwm10, fwm01, fwm11,
                    fe00, fe10, fe01, fe11,
                    bary,
                    triBaseLayers,
                    triSecLayers,
                    triFactLayers,
                    triSecWeights,
                    triFactWeights,
                    triCross,
                    triBaseUse,
                    triSecUse,
                    triHeights,
                    triOptions,
                    triBaseOpts,
                    triSecOpts,
                    triFactOpts,
                    triFactMaxW,
                    triFactEdgeV,
                    triXIdx,
                    triYIdx);

                float3 n = normalize(i.worldNormal);
                float3 surfTriW = ComputeTriWeights(n);
                float topWeight = surfTriW.y;
                float slopeSecond = lerp(1.0 - _SlopeSecondAtten, 1.0, saturate(topWeight * 1.20));
                float slopeAtten = lerp(1.0 - _SlopeFactureAtten, 1.0, saturate(topWeight * 1.35));

                half3 baseAccum = 0;
                float baseTotal = 0;
                AddGroundPassTri(baseAccum, baseTotal, triBaseLayers.x, bary, triBaseLayers, triBaseUse, float3(1.0, 1.0, 1.0), triXIdx, triYIdx, triHeights, triOptions, triBaseOpts, _GroundTiling);
                if (abs(triBaseLayers.y - triBaseLayers.x) > 0.5)
                    AddGroundPassTri(baseAccum, baseTotal, triBaseLayers.y, bary, triBaseLayers, triBaseUse, float3(1.0, 1.0, 1.0), triXIdx, triYIdx, triHeights, triOptions, triBaseOpts, _GroundTiling);
                if (abs(triBaseLayers.z - triBaseLayers.x) > 0.5 && abs(triBaseLayers.z - triBaseLayers.y) > 0.5)
                    AddGroundPassTri(baseAccum, baseTotal, triBaseLayers.z, bary, triBaseLayers, triBaseUse, float3(1.0, 1.0, 1.0), triXIdx, triYIdx, triHeights, triOptions, triBaseOpts, _GroundTiling);
                half4 baseCol = half4(baseTotal > 1e-5 ? baseAccum / max(baseTotal, 1e-5) : half3(0.5,0.5,0.5), 1.0);

                float3 triSecScaled = float3(
                    (abs(triSecLayers.x - triBaseLayers.x) > 0.5) ? ScaleStage2Weight(triSecWeights.x) : 0.0,
                    (abs(triSecLayers.y - triBaseLayers.y) > 0.5) ? ScaleStage2Weight(triSecWeights.y) : 0.0,
                    (abs(triSecLayers.z - triBaseLayers.z) > 0.5) ? ScaleStage2Weight(triSecWeights.z) : 0.0);

                half3 secAccum = 0;
                float secTotal = 0;
                if (triSecScaled.x > 0.0)
                    AddGroundPassTri(secAccum, secTotal, triSecLayers.x, bary, triSecLayers, triSecUse, triSecScaled, triXIdx, triYIdx, triHeights, triOptions, triSecOpts, _SecondaryTiling);
                if (triSecScaled.y > 0.0 && abs(triSecLayers.y - triSecLayers.x) > 0.5)
                    AddGroundPassTri(secAccum, secTotal, triSecLayers.y, bary, triSecLayers, triSecUse, triSecScaled, triXIdx, triYIdx, triHeights, triOptions, triSecOpts, _SecondaryTiling);
                if (triSecScaled.z > 0.0 && abs(triSecLayers.z - triSecLayers.x) > 0.5 && abs(triSecLayers.z - triSecLayers.y) > 0.5)
                    AddGroundPassTri(secAccum, secTotal, triSecLayers.z, bary, triSecLayers, triSecUse, triSecScaled, triXIdx, triYIdx, triHeights, triOptions, triSecOpts, _SecondaryTiling);
                half4 secCol = half4(secTotal > 1e-5 ? secAccum / max(secTotal, 1e-5) : baseCol.rgb, 1.0);

                float avgCrossType = round(dot(triCross, bary));
                float crossingMask = ComputeCrossingMask(avgCrossType, f);
                float secondBlend = saturate(secTotal * _SecondWeightScale * slopeSecond);
                secondBlend *= saturate(0.35 + 0.65 * crossingMask);

                float triFactWeight = dot(triFactWeights, bary);
                float avgFactOpt = dot(triFactOpts, bary);
                float avgFactEdgeV = dot(triFactEdgeV, bary);
                float avgFactMaxWeight = dot(triFactMaxW, bary);
                float factLayer = SelectDominantLayer3(triFactLayers, triFactWeights * bary);
                float factBlend = saturate(triFactWeight * lerp(0.5, 1.0, saturate(avgFactMaxWeight * 1.25)) * _FactureStrength * _FactureWeightScale * slopeAtten);
                float baseLayer = SelectDominantLayer3(triBaseLayers, bary);

                half4 col = baseCol;
                if (_DebugMode < 1.5)
                {
                    col.rgb = half3(0.78, 0.78, 0.78);
                }
                else if (_DebugMode < 2.5)
                {
                    col = baseCol;
                }
                else if (_DebugMode < 3.5)
                {
                    col = lerp(baseCol, secCol, secondBlend);
                }
                else if (_DebugMode < 4.5)
                {
                    col = lerp(baseCol, secCol, secondBlend);
                    if (_FactureEnabled > 0.5 && triFactWeight > 0.001)
                    {
                        half4 factCol = SampleFactureMapped(factLayer, i.localPos, surfTriW, n, _FactureTiling, avgFactOpt, avgFactEdgeV);
                        half3 factTint = lerp(half3(1.0, 1.0, 1.0), factCol.rgb, 0.30);
                        col.rgb = lerp(col.rgb, col.rgb * factTint, factBlend);
                    }
                }
                else if (_DebugMode < 5.5)
                {
                    col.rgb = secondBlend.xxx;
                    col.a = 1.0;
                    return col;
                }
                else if (_DebugMode < 6.5)
                {
                    float mask = (_FactureEnabled > 0.5) ? factBlend : 0.0;
                    col.rgb = mask.xxx;
                    col.a = 1.0;
                    return col;
                }
                else
                {
                    col.rgb = FalseColorFromIndex(baseLayer);
                    col.a = 1.0;
                    return col;
                }

                float3 l = normalize(_FakeLightDir.xyz);
                float ndl = saturate(dot(n, l));
                float lighting = _AmbientStrength + ndl * _DiffuseStrength;
                col.rgb = saturate(col.rgb * lighting * _Brightness);
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
