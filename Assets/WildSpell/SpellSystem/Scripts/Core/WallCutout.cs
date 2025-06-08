// ============================================
// ������ ��� �������� "�����" � �����
// �������� �� ��������� Daniel Ilett
// ============================================
using UnityEngine;
using System.Collections.Generic;

public class WallCutout : MonoBehaviour
{
    [Header("Cutout Settings")]
    public Transform targetObject; // ��������, �� ������� ������
    public LayerMask wallMask = 1; // ���� ���� ��� cutout
    public float cutoutSize = 1f; // ������ �����
    public float falloffSize = 0.5f; // ������ �������� ����

    [Header("Debug")]
    public bool showDebug = false;

    private Camera playerCamera;
    private List<Renderer> affectedWalls = new List<Renderer>();

    // ����� ������� �������
    private static readonly int CutoutPos = Shader.PropertyToID("_CutoutPos");
    private static readonly int CutoutSize = Shader.PropertyToID("_CutoutSize");
    private static readonly int FalloffSize = Shader.PropertyToID("_FalloffSize");

    void Awake()
    {
        // �����: ������ ������ ���� �� ������!
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("WallCutout: ������ ������ ���� �� ������� � Camera!");
        }

        if (targetObject == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetObject = player.transform;
                Debug.Log("WallCutout: ������������� ������ Player");
            }
        }
    }

    void Update()
    {
        if (targetObject == null || playerCamera == null) return;

        // ������� ���������� �����
        ClearCutoutFromWalls();

        // ������� ����� ����� ������� � ����������
        FindWallsBetweenCameraAndTarget();

        // ��������� ������� cutout ��� ���� ��������� ����
        UpdateCutoutPosition();
    }

    private void FindWallsBetweenCameraAndTarget()
    {
        Vector3 cameraPos = playerCamera.transform.position;
        Vector3 targetPos = targetObject.position;
        Vector3 direction = (targetPos - cameraPos).normalized;
        float distance = Vector3.Distance(cameraPos, targetPos);

        // RaycastAll ��� ������ ���� ���� ����� ������� � ����������
        RaycastHit[] hits = Physics.RaycastAll(cameraPos, direction, distance, wallMask);

        if (showDebug)
        {
            Debug.DrawRay(cameraPos, direction * distance, Color.red);
            Debug.Log($"WallCutout: ������� {hits.Length} ���� ����� ������� � ����������");
        }

        // ��������� ��� ��������� ����� � ������
        foreach (RaycastHit hit in hits)
        {
            Renderer wallRenderer = hit.collider.GetComponent<Renderer>();
            if (wallRenderer != null && !affectedWalls.Contains(wallRenderer))
            {
                affectedWalls.Add(wallRenderer);

                if (showDebug)
                    Debug.Log($"WallCutout: ��������� ����� {hit.collider.name}");
            }
        }
    }

    private void UpdateCutoutPosition()
    {
        if (affectedWalls.Count == 0) return;

        // ������������ ������� ��������� � Screen Space
        Vector3 screenPos = playerCamera.WorldToScreenPoint(targetObject.position);

        // ����������� ���������� ������ (0-1)
        Vector2 cutoutPosition = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );

        if (showDebug)
            Debug.Log($"WallCutout: Screen pos = {cutoutPosition}");

        // ��������� �������� ������� ��� ���� affected ����
        foreach (Renderer wallRenderer in affectedWalls)
        {
            if (wallRenderer != null && wallRenderer.material != null)
            {
                Material mat = wallRenderer.material;

                // ������������� �������� cutout
                mat.SetVector(CutoutPos, cutoutPosition);
                mat.SetFloat(CutoutSize, cutoutSize);
                mat.SetFloat(FalloffSize, falloffSize);

                if (showDebug)
                    Debug.Log($"WallCutout: �������� �������� {mat.name}");
            }
        }
    }

    private void ClearCutoutFromWalls()
    {
        // ������� cutout �� ���������� ���� (������������� ������ � 0)
        foreach (Renderer wallRenderer in affectedWalls)
        {
            if (wallRenderer != null && wallRenderer.material != null)
            {
                wallRenderer.material.SetFloat(CutoutSize, 0f);
            }
        }

        affectedWalls.Clear();
    }

    void OnDisable()
    {
        // ������� ��� cutout ��� ����������
        ClearCutoutFromWalls();
    }

    void OnDrawGizmos()
    {
        if (!showDebug || targetObject == null || playerCamera == null) return;

        // ���������� ����� �� ������ � ���������
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerCamera.transform.position, targetObject.position);

        // ���������� ��� affected �����
        Gizmos.color = Color.red;
        foreach (Renderer wall in affectedWalls)
        {
            if (wall != null)
            {
                Gizmos.DrawWireCube(wall.transform.position, wall.bounds.size);
            }
        }
    }
}

// ============================================
// URP SHADER CODE ��� WALL CUTOUT
// �������� ����� Shader Graph ��� ����������� ���� ���
// ============================================

/* 
���������� ��� �������� SHADER GRAPH:

1. �������� ����� URP Lit Shader Graph: 
   Create -> Shader -> Universal Render Pipeline -> Lit Shader Graph

2. �������� ��� Properties:
   - Main Texture (Texture2D)
   - Tiling (Vector2) = (1,1)  
   - Offset (Vector2) = (0,0)
   - Cutout Position (Vector2) = (0.5,0.5) [Reference: _CutoutPos]
   - Cutout Size (Float) = 1.0 [Reference: _CutoutSize] 
   - Falloff Size (Float) = 0.5 [Reference: _FalloffSize]

3. � Graph Settings �������� Alpha Clipping

4. ������ Shader Graph:
   a) Sample Texture 2D � Main Texture -> Base Color
   b) Screen Position -> ��������� �� Screen Width/Height ��� ������������
   c) Distance ����� Screen Position � Cutout Position
   d) ���� Distance < Cutout Size -> Alpha = 0 (cutout)
   e) ���� Distance ����� Cutout Size � (Cutout Size + Falloff) -> ������� �������
   f) ����� Alpha = 1

5. �������� �������� � ���� �������� � ��������� � ������

������������ - ������� .shader ����:
*/

/*
Shader "Custom/WallCutout"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Tiling ("Tiling", Vector) = (1,1,0,0)
        _Offset ("Offset", Vector) = (0,0,0,0)
        _CutoutPos ("Cutout Position", Vector) = (0.5,0.5,0,0)
        _CutoutSize ("Cutout Size", Float) = 1.0
        _FalloffSize ("Falloff Size", Float) = 0.5
    }
    
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="AlphaTest"}
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float4 _MainTex_ST;
            float4 _Tiling;
            float4 _Offset;
            float2 _CutoutPos;
            float _CutoutSize;
            float _FalloffSize;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(output.positionHCS);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv * _Tiling.xy + _Offset.xy);
                
                // Calculate screen space UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Distance from cutout center
                float dist = distance(screenUV, _CutoutPos);
                
                // Alpha cutout with falloff
                float cutoutEdge = _CutoutSize + _FalloffSize;
                float alpha = smoothstep(_CutoutSize, cutoutEdge, dist);
                
                col.a *= alpha;
                
                // Alpha test
                if(col.a < 0.5) discard;
                
                return col;
            }
            ENDHLSL
        }
    }
}
*/