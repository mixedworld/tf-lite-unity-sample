/*	This shader samples _MainTex and returns the colour immediately. As an image
	effect, this shader does nothing; it is a skeleton to build effects on.
*/
Shader "SMO/Complete/Base"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
		ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

			/*	Sample the screen texture at the pixel position and return the
				colour immediately.
			*/
            fixed4 frag (v2f_img i) : SV_Target
            {
				float2 flippedUVs = i.uv;
				flippedUVs.x = flippedUVs.x;
				flippedUVs.y = 1.0 - flippedUVs.y;
				fixed4 tex = tex2D(_MainTex, flippedUVs);
				//fixed4 tex = tex2D(_MainTex, i.uv);
				return tex;
            }
            ENDCG
        }
    }
}
