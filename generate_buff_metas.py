import os
import uuid

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {bx}, y: {by}, z: {bz}, w: {bw}}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

FOLDER_META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def make_guid():
    return uuid.uuid4().hex

def ensure_folder_meta(folder_path):
    meta_path = folder_path + ".meta"
    if not os.path.exists(meta_path):
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(FOLDER_META_TEMPLATE.format(guid=make_guid()))
        print(f"Created folder meta: {meta_path}")

def generate_metas():
    ensure_folder_meta(r"F:\unity\Farm-Beware\Assets\Textures\Icons\Buffs")
    ensure_folder_meta(r"F:\unity\Farm-Beware\Assets\Resources\Icons")
    ensure_folder_meta(r"F:\unity\Farm-Beware\Assets\Resources\Icons\Buffs")

    dirs = [
        r"F:\unity\Farm-Beware\Assets\Textures\Icons\Buffs",
        r"F:\unity\Farm-Beware\Assets\Resources\Icons\Buffs"
    ]
    for d in dirs:
        for fname in os.listdir(d):
            if fname.endswith(".png"):
                full_path = os.path.join(d, fname)
                meta_path = full_path + ".meta"
                
                # Check if it's a 9-slice frame texture
                if "Buff_Frame" in fname or "Buff_Cooldown" in fname:
                    bx, by, bz, bw = 18, 18, 18, 18
                else:
                    bx, by, bz, bw = 0, 0, 0, 0
                    
                # Preserve existing GUID if meta already exists
                existing_guid = None
                if os.path.exists(meta_path):
                    with open(meta_path, "r", encoding="utf-8") as f:
                        for line in f:
                            if line.startswith("guid:"):
                                existing_guid = line.split(":", 1)[1].strip()
                                break
                                
                file_guid = existing_guid if existing_guid else make_guid()
                sprite_id = make_guid()
                content = META_TEMPLATE.format(guid=file_guid, sprite_id=sprite_id, bx=bx, by=by, bz=bz, bw=bw)
                with open(meta_path, "w", encoding="utf-8") as f:
                    f.write(content)
                print(f"Wrote meta (border={bx}): {meta_path}")

if __name__ == "__main__":
    generate_metas()
