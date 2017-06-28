/*!
* @file Alpha.shader
* @brief 透過シェーダ
* @date 2017/05/21
* @author 仁科香苗
* @note 参考:Qiita(http://qiita.com/beinteractive/items/fc80a42388581473db4d)
*/
Shader "Custom/Alpha" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader{
		Tags{
		"Queue" = "Transparent"
		"RenderType" = "Transparent"
	}
		CGPROGRAM
#pragma surface surf Lambert alpha

		sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		half4 c = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = c.rgb;
		o.Alpha = 0.25;
	}
	ENDCG
	}
	
}
