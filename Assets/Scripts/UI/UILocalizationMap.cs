using System.Collections.Generic;

/// <summary>
/// 게임 내 모든 UI 텍스트의 한국어/영어 원문 → String Table 키 매핑.
/// AutoLocalizePanel / OptionsPanelAutoLocalize가 공통으로 참조한다.
/// 새 UI 텍스트는 여기에 한 줄 추가 + CSV에 한 줄 추가만 하면 자동 번역됨.
/// </summary>
public static class UILocalizationMap
{
    public static readonly Dictionary<string, string> TextToKey = new()
    {
        // ── 옵션 패널 ─────────────────────────────────────
        { "옵션", "ui.options.title" },
        { "Options", "ui.options.title" },
        { "■ 볼륨", "ui.options.volume_section" },
        { "■ Volume", "ui.options.volume_section" },
        { "마스터", "ui.options.master" },
        { "Master", "ui.options.master" },
        { "마스터 음량", "ui.options.master_volume" },
        { "Master Volume", "ui.options.master_volume" },
        { "BGM", "ui.options.bgm" },
        { "BGM 음량", "ui.options.bgm_volume" },
        { "BGM Volume", "ui.options.bgm_volume" },
        { "효과음", "ui.options.sfx" },
        { "SFX", "ui.options.sfx" },
        { "■ 화면설정", "ui.options.display_section" },
        { "■ Display", "ui.options.display_section" },
        { "해상도", "ui.options.resolution" },
        { "Resolution", "ui.options.resolution" },
        { "화면 모드", "ui.options.fullscreen" },
        { "Screen Mode", "ui.options.fullscreen" },
        { "언어", "ui.options.language" },
        { "Language", "ui.options.language" },
        { "■ 조작 키", "ui.options.controls_section" },
        { "■ Controls", "ui.options.controls_section" },
        { "대시", "ui.options.dash" },
        { "Dash", "ui.options.dash" },
        { "공격", "ui.options.attack" },
        { "Attack", "ui.options.attack" },
        { "인벤토리", "ui.options.inventory" },
        { "Inventory", "ui.options.inventory" },
        { "상호작용", "ui.options.interact" },
        { "Interact", "ui.options.interact" },
        { "변경", "ui.options.rebind" },
        { "Rebind", "ui.options.rebind" },
        // ── 인벤토리 / 장비 ────────────────────────────────
        { "장비", "ui.inv.equipment" },
        { "Equipment", "ui.inv.equipment" },
        { "가방", "ui.inv.backpack" },
        { "Bag", "ui.inv.backpack" },
        { "무기", "ui.inv.weapon" },
        { "Weapon", "ui.inv.weapon" },
        { "방어구  & 장신구", "ui.inv.armor_accessory" },
        { "Armor & Accessory", "ui.inv.armor_accessory" },
        { "닫기  [A]", "ui.inv.close" },
        { "Close [A]", "ui.inv.close" },
        { "TAB to close", "ui.inv.tab_close" },
        // ── 상점 ───────────────────────────────────────
        { "상점", "ui.shop.title" },
        { "Shop", "ui.shop.title" },
        { "구매", "ui.shop.buy" },
        { "Buy", "ui.shop.buy" },
        { "뒤로", "ui.shop.back" },
        { "Back", "ui.shop.back" },
        // ── 일시정지 / 확인 다이얼로그 ─────────────────────
        { "일시정지", "ui.pause.title" },
        { "Paused", "ui.pause.title" },
        { "게임종료하시겠습니까?", "ui.pause.quit_confirm" },
        { "Quit the game?", "ui.pause.quit_confirm" },
        { "새로 시작하시겠습니까?", "ui.pause.new_run_confirm" },
        { "Start a new run?", "ui.pause.new_run_confirm" },
        { "타이틀로 돌아가시겠습니까?", "ui.pause.return_title_confirm" },
        { "Return to title?", "ui.pause.return_title_confirm" },
        { "네", "ui.common.yes" },
        { "Yes", "ui.common.yes" },
        { "아니오", "ui.common.no" },
        { "아니요", "ui.common.no" },
        { "No", "ui.common.no" },
        // ── 게임 클리어 / 오버 ──────────────────────────
        { "GAME OVER", "ui.gameover.title" },
        { "GAME CLREA!", "ui.clear.title" },
        { "GAME CLEAR!", "ui.clear.title" },
        // 통계 키(ui.stats.*)는 GameClearUI/GameOverUI에서 L10n.Format으로 직접 처리하므로
        // 여기에 등록하지 않는다. 등록하면 AutoLocalizePanel이 포맷 문자열("{0}")로 덮어쓴다.
        // ── HUD ───────────────────────────────────────
        { "골드: 0", "ui.hud.gold" },
        { "Gold: 0", "ui.hud.gold" },
    };
}
