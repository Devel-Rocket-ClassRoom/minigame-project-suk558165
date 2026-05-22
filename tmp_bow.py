from PIL import Image
import numpy as np
from scipy import ndimage

base_img  = r'C:\Users\junyoung\source\repos\minigame-project-suk558165\Assets\Imported\EnemySprites\BowEnemy'
base_anim = r'C:\Users\junyoung\source\repos\minigame-project-suk558165\Assets\Animations\Enemy\BowEnemy'

atk_ids = [431490331, -910404177, -210782873, 145627527,
           1448825686, 1527955861, 1509943786, -770067316,
           251678404, -1851946578, 999630298, -1483526692,
           -1526900063, 282465308, -1671414416, -1806458253]
walk_ids = [126010077, 1436008079, -1706231383, 579856159,
            1477245556, 317894990, -233966677, -899091129,
            -1938062788, 797396402, -1297623436, 1271610581,
            1026601744, -921460435, 1185544225, -882227238]
death_ids = [283617775, 1195667126, -1428186243, -78264032,
             -1966379399, -1176143545, -1158530835, 841657678,
             1525292555, -167656637, 1123431217, -1663393999]

def alpha_clean(arr):
    r = arr.copy(); r[r[:,:,3] < 50, 3] = 0; return r

def remove_sparkle(arr):
    """Remove grey/silver sparkle: very low saturation bright pixels."""
    r,g,b,a = (arr[:,:,i].astype(float) for i in range(4))
    avg = (r+g+b)/3
    max_c = np.maximum(np.maximum(r,g),b)
    min_c = np.minimum(np.minimum(r,g),b)
    sat = (max_c - min_c) / (max_c + 1)
    # Grey sparkle: sat < 0.05, brightness > 80
    sparkle = (sat < 0.05) & (avg > 80) & (a > 50)
    if not sparkle.any():
        return arr
    # Grow sparkle mask slightly to clean edges
    sparkle_grown = ndimage.binary_dilation(sparkle, iterations=2)
    result = arr.copy()
    result[sparkle_grown, 3] = 0
    return result

def build_sheet(src_path, row_bands, out_path, scale):
    img  = Image.open(src_path).convert('RGBA')
    arr  = np.array(img); w = img.size[0]; cw = w // 4
    col_bounds = [(i*cw, i*cw+cw if i<3 else w) for i in range(4)]
    out = Image.new('RGBA', (1024,1024), (0,0,0,0))
    for i, (y1,y2) in enumerate(row_bands):
        for j, (x1,x2) in enumerate(col_bounds):
            cell = remove_sparkle(alpha_clean(arr[y1:y2+1, x1:x2].copy()))
            fr   = Image.fromarray(cell)
            fw, fh = fr.size
            ow = min(round(fw*scale),256); oh = min(round(fh*scale),256)
            scaled = fr.resize((ow,oh), Image.NEAREST)
            canvas = Image.new('RGBA',(256,256),(0,0,0,0))
            canvas.paste(scaled, ((256-ow)//2, max(256-oh,0)))
            idx = i*4+j
            out.paste(canvas, ((idx%4)*256,(idx//4)*256))
    out.save(out_path); print(f'Saved: {out_path}')

def write_meta(path, guid, prefix, ids):
    lines = ['fileFormatVersion: 2',f'guid: {guid}','TextureImporter:','  internalIDToNameTable:']
    for i,iid in enumerate(ids):
        lines += ['  - first:',f'      213: {iid}',f'    second: {prefix}_{i}']
    lines += ['  externalObjects: {}','  serializedVersion: 13',
        '  mipmaps:','    mipMapMode: 0','    enableMipMap: 0','    sRGBTexture: 1',
        '    linearTexture: 0','    fadeOut: 0','    borderMipMap: 0',
        '    mipMapsPreserveCoverage: 0','    alphaTestReferenceValue: 0.5',
        '    mipMapFadeDistanceStart: 1','    mipMapFadeDistanceEnd: 3',
        '  bumpmap:','    convertToNormalMap: 0','    externalNormalMap: 0',
        '    heightScale: 0.25','    normalMapFilter: 0','    flipGreenChannel: 0',
        '  isReadable: 0','  streamingMipmaps: 0','  streamingMipmapsPriority: 0',
        '  vTOnly: 0','  ignoreMasterTextureLimit: 0','  grayScaleToAlpha: 0',
        '  generateCubemap: 6','  cubemapConvolution: 0','  seamlessCubemap: 0',
        '  textureFormat: 1','  maxTextureSize: 2048',
        '  textureSettings:','    serializedVersion: 2','    filterMode: 0',
        '    aniso: 1','    mipBias: 0','    wrapU: 1','    wrapV: 1','    wrapW: 1',
        '  nPOTScale: 0','  lightmap: 0','  compressionQuality: 50',
        '  spriteMode: 2','  spriteExtrude: 1','  spriteMeshType: 1',
        '  alignment: 0','  spritePivot: {x: 0.5, y: 0.5}',
        '  spritePixelsToUnits: 100','  spriteBorder: {x: 0, y: 0, z: 0, w: 0}',
        '  spriteGenerateFallbackPhysicsShape: 1','  alphaUsage: 1',
        '  alphaIsTransparency: 1','  spriteTessellationDetail: -1',
        '  textureType: 8','  textureShape: 1','  singleChannelComponent: 0',
        '  flipbookRows: 1','  flipbookColumns: 1','  maxTextureSizeSet: 0',
        '  compressionQualitySet: 0','  textureFormatSet: 0',
        '  ignorePngGamma: 0','  applyGammaDecoding: 0','  cookieLightType: 0',
        '  platformSettings:','  - serializedVersion: 3',
        '    buildTarget: DefaultTexturePlatform','    maxTextureSize: 2048',
        '    resizeAlgorithm: 0','    textureFormat: -1','    textureCompression: 1',
        '    compressionQuality: 50','    crunchedCompression: 0',
        '    allowsAlphaSplitting: 0','    overridden: 0',
        '    androidETC2FallbackOverride: 0',
        '    forceMaximumCompressionQuality_BC6H_BC7: 0',
        '  spriteSheet:','    serializedVersion: 2','    sprites:']
    for i,iid in enumerate(ids):
        c,ro = i%4,i//4; x=c*256; y=(3-ro)*256
        sid=f'4342{i:02d}303030303030303030303030'
        lines += ['    - serializedVersion: 2',f'      name: {prefix}_{i}',
            '      rect:','        serializedVersion: 2',
            f'        x: {x}',f'        y: {y}','        width: 256','        height: 256',
            '      alignment: 0','      pivot: {x: 0.5, y: 0.5}',
            '      border: {x: 0, y: 0, z: 0, w: 0}',
            '      outline: []','      physicsShape: []','      tessellationDetail: -1',
            '      bones: []',f'      spriteID: {sid}',f'      internalID: {iid}',
            '      vertices: []','      indices:','      edges: []','      weights: []',
            '      secondaryTextures: []','      nameFileIdTable: {}']
    lines += ['    outline: []','    physicsShape: []','    bones: []','    spriteID:',
        '    internalID: 0','    vertices: []','    indices:','    edges: []',
        '    weights: []','    secondaryTextures: []','    nameFileIdTable: {}',
        '  spritePackingTag:','  pSDRemoveMatte: 0','  userData:',
        '  assetBundleName:','  assetBundleVariant:']
    with open(path,'w',encoding='utf-8',newline='\n') as f:
        f.write('\n'.join(lines)+'\n')
    print(f'Written meta: {path}')

def write_anim(path, name, guid, ids, loop, stop_time, events=None):
    curve, mapping = [], []
    for n,iid in enumerate(ids):
        t=f'{n/12:.7g}'
        curve.append(f'    - time: {t}')
        curve.append(f'      value: '+'{'+ f'fileID: {iid}, guid: {guid}, type: 3'+'}')
        mapping.append('    - '+'{'+ f'fileID: {iid}, guid: {guid}, type: 3'+'}')
    ev_lines = []
    if events:
        for ev_t,fn in events:
            ev_lines += [f'  - time: {ev_t}',f'    functionName: {fn}',
                '    data: ','    objectReferenceParameter: {fileID: 0}',
                '    floatParameter: 0','    intParameter: 0','    messageOptions: 0']
    ev_block = '\n'.join(ev_lines) if ev_lines else '  []'
    parts = ['%YAML 1.1','%TAG !u! tag:unity3d.com,2011:',
        '--- !u!74 &7400000','AnimationClip:',
        '  m_ObjectHideFlags: 0','  m_CorrespondingSourceObject: {fileID: 0}',
        '  m_PrefabInstance: {fileID: 0}','  m_PrefabAsset: {fileID: 0}',
        f'  m_Name: {name}','  serializedVersion: 7','  m_Legacy: 0',
        '  m_Compressed: 0','  m_UseHighQualityCurve: 1',
        '  m_RotationCurves: []','  m_CompressedRotationCurves: []',
        '  m_EulerCurves: []','  m_PositionCurves: []','  m_ScaleCurves: []',
        '  m_FloatCurves: []','  m_PPtrCurves:','  - serializedVersion: 2','    curve:',
        ]+curve+[
        '    attribute: m_Sprite','    path: ','    classID: 212',
        '    script: {fileID: 0}','    flags: 2',
        '  m_SampleRate: 12','  m_WrapMode: 0','  m_Bounds:',
        '    m_Center: {x: 0, y: 0, z: 0}','    m_Extent: {x: 0, y: 0, z: 0}',
        '  m_ClipBindingConstant:','    genericBindings:','    - serializedVersion: 2',
        '      path: 0','      attribute: 0','      script: {fileID: 0}',
        '      typeID: 212','      customType: 23','      isPPtrCurve: 1',
        '      isIntCurve: 0','      isSerializeReferenceCurve: 0',
        '    pptrCurveMapping:',
        ]+mapping+[
        '  m_AnimationClipSettings:','    serializedVersion: 2',
        '    m_AdditiveReferencePoseClip: {fileID: 0}',
        '    m_AdditiveReferencePoseTime: 0','    m_StartTime: 0',
        f'    m_StopTime: {stop_time}','    m_OrientationOffsetY: 0',
        '    m_Level: 0','    m_CycleOffset: 0','    m_HasAdditiveReferencePose: 0',
        f'    m_LoopTime: {1 if loop else 0}',
        '    m_LoopBlend: 0','    m_LoopBlendOrientation: 0',
        '    m_LoopBlendPositionY: 0','    m_LoopBlendPositionXZ: 0',
        '    m_KeepOriginalOrientation: 0','    m_KeepOriginalPositionY: 1',
        '    m_KeepOriginalPositionXZ: 0','    m_HeightFromFeet: 0','    m_Mirror: 0',
        '  m_EditorCurves: []','  m_EulerEditorCurves: []',
        '  m_HasGenericRootTransform: 0','  m_HasMotionFloatCurves: 0',
        '  m_Events:', ev_block]
    with open(path,'w',encoding='utf-8',newline='\n') as f:
        f.write('\n'.join(parts)+'\n')
    print(f'Written anim: {path}')

# ── Build everything ──────────────────────────────────────────────────────────
build_sheet(f'{base_img}/BowAttack.png',
            [(0,124),(125,249),(250,374),(375,499)],
            f'{base_img}/BowEnemy_Attack.png', 256/125)
build_sheet(f'{base_img}/BowWalk.png',
            [(0,124),(125,249),(250,374),(375,499)],
            f'{base_img}/BowEnemy_Walk.png', 256/125)
build_sheet(f'{base_img}/BowDeath.png',
            [(0,249),(253,374),(433,499)],
            f'{base_img}/BowEnemy_Death.png', 256/250)

write_meta(f'{base_img}/BowEnemy_Attack.png.meta','c13b952b39fb50f46a202aef9163c02b','BAtk',atk_ids)
write_meta(f'{base_img}/BowEnemy_Walk.png.meta','7dcf81f3ee28f0641baf86a543203c50','BWlk',walk_ids)
write_meta(f'{base_img}/BowEnemy_Death.png.meta','5e63eb279e2ad984f96302108c43e49c','BDth',death_ids)

write_anim(f'{base_anim}/BowEnemy_Attack.anim','BowEnemy_Attack_Final',
           'c13b952b39fb50f46a202aef9163c02b', atk_ids, loop=False, stop_time='1.3333334',
           events=[(0.25,'EnableHitbox'),(0.67,'DisableHitbox')])
write_anim(f'{base_anim}/BowEnemy_Walk.anim','BowEnemy_Walk_Final',
           '7dcf81f3ee28f0641baf86a543203c50', walk_ids, loop=True, stop_time='1.3333334')
write_anim(f'{base_anim}/BowEnemy_Death.anim','BowEnemy_Death_Final',
           '5e63eb279e2ad984f96302108c43e49c', death_ids, loop=False, stop_time='1.0')

# ── Final check f15 Walk ──────────────────────────────────────────────────────
sh = Image.open(f'{base_img}/BowEnemy_Walk.png')
f15 = sh.crop((3*256,3*256,4*256,4*256))
bg  = Image.new('RGBA',(256,256),(255,255,255,255)); bg.paste(f15,mask=f15)
bg.save(f'{base_img}/_Walk_f15_clean.png')
arr_f=np.array(f15); a=arr_f[:,:,3]
r,g,b=(arr_f[:,:,i].astype(float) for i in range(3))
max_c=np.maximum(np.maximum(r,g),b); min_c=np.minimum(np.minimum(r,g),b)
sat=(max_c-min_c)/(max_c+1); avg=(r+g+b)/3
grey=((sat<0.05)&(avg>80)&(a>50)).sum()
print(f'Walk f15 grey pixels remaining: {grey} (should be 0)')
print('All done!')
