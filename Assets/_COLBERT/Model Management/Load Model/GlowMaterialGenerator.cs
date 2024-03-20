using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//generate materials for models loaded with ModelLoader (materials with glow effect for hovering/selecting)
public class GlowMaterialGenerator : IMaterialGenerator
{
    private Material material = null;
    private Material materialDouble = null;

    public GlowMaterialGenerator(Material material, Material materialDouble)
    {
        this.material = material;
        this.materialDouble = materialDouble;
    }

    public Material GenerateMaterial(GLTFast.Schema.MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
    {
        Material material = gltfMaterial.doubleSided ? new Material(this.materialDouble) : new Material(this.material);
        material.name = gltfMaterial.name;

        Color baseColorLinear = Color.white;

        if (gltfMaterial.Extensions != null)
        {
            // Specular glossiness
            GLTFast.Schema.PbrSpecularGlossiness specGloss = gltfMaterial.Extensions.KHR_materials_pbrSpecularGlossiness;
            if (specGloss != null)
            {
                baseColorLinear = specGloss.DiffuseColor;
            }
        }

        if (gltfMaterial.PbrMetallicRoughness != null
            // If there's a specular-glossiness extension, ignore metallic-roughness
            // (according to extension specification)
            && gltfMaterial.Extensions?.KHR_materials_pbrSpecularGlossiness == null)
        {
            baseColorLinear = gltfMaterial.PbrMetallicRoughness.BaseColor;
        }

        material.SetColor("_BaseColor", baseColorLinear);

        return material;
    }

    public Material GetDefaultMaterial(bool pointsSupport = false)
    {
        return new Material(this.material);
    }

    public void SetLogger(ICodeLogger logger)
    {

    }
}
