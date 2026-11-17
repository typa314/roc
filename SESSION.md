# 《國光：反攻大陸》設計討論紀錄 (SESSION.md)

## 2026-11-11 — 科技升級節奏拍板與實作結論

### 使用者裁決
1. 移除「一鍵瞬間完成」按鈕，所有 40 項科技一律**統一按天累計完成**。
2. 保留**專案趕工**且**不設冷卻時間**，不須超高昂一次性代價，只需增加研發預算成本%（定為 `+40%` 科研成本換取 `+50%` 研發速度）。
3. **底部工作列**平時維持乾淨，只以通知方式顯示已完成的科技項目（`★ 科技研發完成通知：【科技名】（日期）`）。
4. 全面移除所有「史實」「架空」提示字樣（含科技節點、事件標籤、選項字尾與卡片立場徽章）。

### 核心變更落地（`SimEngine.cs` / `MainForm.cs` / `EventSystem.cs` / `GameData.cs`）
- `SimEngine.StepResearch(s, dtDay, chargeDailySurcharge: true)` 於每日推進執行；開啟 `ResearchRush` 時每日計收 `(ResCost - BaseResCost) * dtDay`（`+40%` 科研費），同時 `RpPerTurn` 提升 `+50%`。
- 新增 `EstDaysRemaining(s, id)` 換算剩餘天數，於節點卡、頁首摘要與底部細節欄顯示。
- `GrantTech` / `StepResearch` 於完成日寫入 `LastTechCompleted` 與 `LastTechDate`；`MainForm.RefreshAllUi` 僅於有完成通知時渲染 `_lblDockTech`。

## 2026-11-11 — 地圖滾輪縮放與拖曳平移支援
- `MainForm` 實作 `IMessageFilter`（截獲 `WM_MOUSEWHEEL` 轉至地圖畫布並排除浮動視窗／抽屜／科技樹／軍情處遮罩），修正視窗按鈕持有焦點時滾輪無法縮放地圖的問題。
- `GetMapViewport` 結合 `_mapZoom`（`1.0f～4.0f`）與游標錨點平移計算 `_mapPanX`/`_mapPanY`（`ClampMapPan`），搭配 `MouseDown/MouseMove/MouseUp` `>= 6px` 拖曳門檻支援放大後平移，`MapPanel_Paint` 將頂部警示與圖例鎖定於螢幕座標最上層並新增右下角縮放控制條（`－ / 倍率 / ＋ / 重設`、`R` 鍵重設）。

## 2026-11-11 — 二戰復古兵棋圖桌全介面視覺重製（`WarUiControls.cs` + `MainForm.cs` + `GameData.cs`）
- **設計決策**：依使用者選定方向，全面重製為**二戰復古兵棋圖桌**風格（深煙燻核桃木、做舊黃銅、羊皮紙暖白墨色、普魯士灰藍與復古硃紅），並根治傳統 WinForms 直角按鈕、刺眼亮白色系統捲軸、科技樹美感與軍情處留白過寬問題。
- **自繪基礎元件（`src\WarUiControls.cs`）**：建立 `WarPalette`、`WarGdi.DrawPlaqueFrame`、`WarRoomPanel`（漸層＋銅釘圓角銘牌）、`WarButton`（徹底取代預設 `Button` 的圓角漸層黃銅按鈕）與 `DarkScrollContainer`（內部 `_viewportHost` 裁剪視窗＋自繪 11px 黃銅暗色捲軸，消滅科技樹、軍情處、側抽屜（含移除 `RichTextBox`）與左下兵種卡所有白色捲軸，並於 `MainForm.PreFilterMessage` 優先接管滾輪捲動）。
- **地圖、HUD 與專頁升級（`MainForm.cs`、`GameData.cs`）**：
  - 頂部 HUD（56px）與底部導航列（50px）換裝漸層核桃木＋雙重黃銅飾線；左下兵種卡微調為 416×214px 防裁切。
  - 地圖新增三層海岸淺海暈染、羅盤水印、桌面暗角、柔化象牙省界／雙層軍區線與圓角陰影籌碼；微調台澎金馬大陳舟山引線座標減低 `1.0×` 標記鄰接。
  - 科技樹全面升級為工程方格圖桌、時代卷軸標題、平滑三次貝茲連線（`DrawBezier`）與圓角進度條；軍情處重排為左欄「2×2 篩選＋三張附動態能量條戰區簡報」＋右欄「卷宗時間軸」雙欄對稱排版。

## 2026-11-11 — 地圖縮放文字與兵牌比例分離（螢幕清晰恆定字級）
- **問題定位**：原先 `MapPanel_Paint`、`DrawSeaZones`、`DrawMovementArrows`、`DrawNodesAndCounters`、`DrawUnitCounter` 與 `HitTestMap` 將字級、籌碼寬高與海域框尺寸線性乘以 `scale = vp.Width / MapW`（隨 `_mapZoom` 放大至最高 `4.0×`），導致滾輪放大時字體與兵牌同步過度膨脹。
- **工程修正（`src\MainForm.cs`、`src\Program.cs`、`DESIGN.md`）**：
  - 新增 `GetMapUiScale()`（`GetBaseMapViewport().Width / GameData.MapW`），徹底分離「地理空間縮放（`scale`）」與「戰術介面與地圖文字比例（`uiScale`）」。
  - 地圖文字（省名暗紋、鄰國名稱、據點名牌、籌碼兵牌、預估勝算徽章、進軍箭頭標籤、海域戰術框與經緯刻度）全面維持 `uiScale` 螢幕固定清晰字級；邊線／海岸暈染與羅盤線條柔限於 `strokeScale <= 1.45 * uiScale`。
  - 同步更新 `HitTestMap`（海空框點擊區、據點半徑）與 `LayoutFloatingMapPanels`（`_zoneCard` 錨點）改用 `uiScale`，並於 `--test` 驗證放大至 `>1.5×` 後 `CurrentMapUiScaleForTest` 不隨縮放膨脹且實測截圖 `ui_zoom_snapshot.png` 確認沿海據點與文字清晰不重疊。

## 2026-11-11 — 修復月結戰役海域字典例外（'Z3'）＋ 事件卡去誘導與一般／簡易模式
- **修復重大閃退 Bug（`KeyNotFoundException: The given key 'Z3' was not present in the dictionary`）**：
  - 根因：`SimEngine.MonthlyTick` 於有陸戰發生時（`rep.BattleNodes.Count > 0`），原邏輯將駐防台海 `Z3`／東海 `Z2` 的艦隊與航空聯隊判為 `home = false`，隨後進入 `s.Control[u.Loc]` 查表（`s.Control`/`s.Garrison` 僅含 22 個陸地節點）。
  - 修正與驗證：全面盤點全案 `u.Loc`／`Control`／`Garrison` 索引點，依 **G3 先重現失敗（`MonthlyTick` 拋出 `Z3` 例外退出碼 1）再修復**；於 `MonthlyTick` 分離海空／兩棲域（不進交戰海域即可恢復戰力，絕不查地形字典）與陸軍域（改採 `TryGetValue`），`4B` 迴歸測試通過。
- **事件卡介面調整（`MainForm.cs`、`SimEngine.cs`、`EventSystem.cs`）**：
  - 移除事件視窗標題列的「【二元抉擇 · 雙卡戰略對決】」「【重大事件 · 三選一戰略圖卡】」「【外交衝擊 · 緊急應變處置】」等字樣。
  - 新增 `GameState.EasyMode` 與主選單「◆ 一般模式（預設 · 無路線誘導）」／「★ 簡易模式（顯示方針提示）」切換按鈕（可隨時返主選單切換）。一般模式下隱藏「主力方案／替代方針／非常路線／高風險決策」與選項字尾 `（高風險）` 並統一使用中性邊框色；簡易模式保留原有輔助提示。

## 2026-11-11 — 調兵流程「重戰略、少微操」大幅簡化
- **核心原則（`SimEngine.cs`、`MainForm.cs`、`Program.cs`、`DESIGN.md`）**：
  - **我方控制港島調防免運力**：於 `CheckMove` 與 `MonthlyTick` 區分「我方港島調防（非敵區）」與「跨海進攻搶灘（敵區）」；我方港口與外島之間（含台灣／澎湖／金門 ↔ 海南等）調防免消耗進攻兩棲運力且不鎖單一海域，`MonthlyTick` 途中耗損大幅降低（基礎 `4%`）；進攻大陸／敵區則維持共有海域與兩棲運力檢定。
  - **自動護航 ＋ 一鍵海空佈防**：新增 `AutoDeployReserveEscorts`（出兵跨海時自動從台灣預備隊派駐缺口艦隊／航空聯隊至該海域）與 `AutoBalanceNavalAir`（地圖右下「`⚓ 一鍵海空佈防`」按鈕依威脅與戰區優先序一鍵自動均衡海空軍）。
  - **開局預選精銳 ＋ 共用「`左鍵點圖派兵`」（不為各省建按鈕）**：移除依目標地區動態生成一排派兵按鈕之設計（避免每個地區駐軍都多一個按鍵造成版面膨脹與操作割裂）；改為在 `StartScenario` / `StartFromCheckpoint` / `LoadGame` 開局時預選台灣本島前 3 師待命部隊（`選 3/14`，海南島等所有可達據點同步高亮可調防），並於左下指戰卡保留單一共用切換鈕「`左鍵點圖派兵`」（點擊切換為「`✕ 取消點圖`」後可用左鍵直點地圖任何亮圈據點出兵），搭配原有右鍵點擊／右鍵拖線直覺操作。
- **後續修正（2026-11-11）：戰時科技研發節奏大幅加速（`SimEngine.RpPerTurnFor`）**：
  - 原 `s.ResLevel * (3.0 + s.Gdp / 260.0) + s.RpBonus`（1950 Lv.1 約 `3.05 RP/yr`）導致 T0 科技需 `479–837 天`（1.3～2.3 年）、T1 高階項達 `1,196 天`，在緊湊戰局下過慢。
  - 校準為戰時節奏 `s.ResLevel * (18.0 + s.Gdp / 60.0) + s.RpBonus * 2.5`（1950 Lv.1 約 `18.23 RP/yr`）：T0 `Cost 4–6` 縮短為 `81–121 天`（趕工 `54–81 天`），標準戰時預算 `Lv.2`（`36.47 RP/yr`）下 `Cost 4–10` 僅需 `41–101 天`（趕工 `27–67 天`）。
  - **驗證狀態（Rule G5）**：暫存目錄獨立建置 `verify_build`（0 警告、0 錯誤）與 `--test` 全數通過（`TEST_EXIT_CODE=0`，已檢查 `ui_1950_snapshot.png` 與 `ui_tech_snapshot.png` 後刪除 `verify_build`）；惟正式產物 `bin\Release\net8.0-windows\國光：反攻大陸.exe` 因被執行中視窗（`PID 102084`）鎖定，尚未覆寫更新與就地重測。

## 2026-11-11 — 修正科技樹頁面因待決事件被迫關閉跳回的問題
- **根因（G1）**：`MainForm.UpdateOverlays` 在 `State.PendingEvents.Count > 0` 時無條件呼叫 `if (_techPageOpen) CloseTechPage();`，且 `OpenTechPage` 未於釋放其他浮窗前優先停表；導致只要有待決事件或切換當下觸發事件（並透過 `RefreshAllUi()` 重整），科技樹專頁即被強制關閉並跳回地圖。
- **修正與 G3 驗證**：
  - `OpenTechPage` 入口處第一時間設定 `_techPageOpen = true` 並 `SyncSimTimer()` 停表。
  - `UpdateOverlays` 與 `SetGameMode` 改為 `State.PendingEvents.Count > 0 && !_techPageOpen` 才彈出事件視窗，停留於科技樹期間絕不強制關閉跳回；`CloseTechPage` 主動退出後立即呼叫 `UpdateOverlays()` 接手顯示待決事件。
  - `--test` 補入 G3 先失敗（`EXIT_CODE=1` 重現被跳回）再通過（`TEST_EXIT_CODE=0`）之自動化迴歸測試。

## 2026-11-11 — 修正跨科技分頁按研發後先前項目消失、須重新跑的問題
- **根因（G1）**：`SimEngine.SetResearch` 原先僅維持單一全域 `ResearchId` 且未設排程佇列與進度保存（每次切換即 `s.ResearchId = id; s.ResearchProg *= 0.5;`），在五大分頁（如 1950 唯一可研的「科技 / 後勤 / 情蒐」）按鈕皆顯示 `▶ 啟動研發`、無分頁 `★` 標記的情況下，玩家於科技按下研發再切至後勤／情蒐按下項目、切回科技時，原先項目已被靜默覆蓋重置為「未研發」。且 `active` 卡片在進度 `0.00` 時條塊無明確 `研發中` 字樣與高亮底條。
- **修正與 G3 驗證**：
  - `SimEngine.cs` 新增 `ResearchQueue`（自動按順位接續）與 `ResearchProgMap`（100% 保留切換／暫停項目的已累積進度，不再歸零減半），並實作 `ClickTechAction`、`SavedResearchProg` 及 `StepResearch` 完成自動接續。
  - `MainForm.cs` 左鍵支援啟動或「`＋ 加入排程`（`◷ 排程 #N · 點擊移出`）」、右鍵立即改研並把原項目保留進度退回排程 `#2`；分頁按鈕顯示 `★`（研發中）與 `◷`（排程中），活躍卡明確繪製 `⚙ 研發中` 與進度導引條。
  - `--test` 補入實機按鈕座標點擊驗證（科技啟動 `耕者有其田` → 後勤排入 `中字號` → 情蒐排入 `外島雷達` → 切至軍情處 → 切回科技仍為研發中 → 右鍵改研保留 2.00 點進度並於完成後自動接續），於暫存目錄 `verify_build` 通過 Release 編譯（0 警告、0 錯誤）、`TEST_EXIT_CODE=0` 及 `ui_tech_science_snapshot.png` / `ui_tech_snapshot.png` 截圖檢查。
  - **未完成（Rule G5）**：因使用者於 `18:58:57` 重開的舊版 `國光：反攻大陸.exe`（`PID 80544`）仍鎖定 `bin\Release\net8.0-windows\國光：反攻大陸.exe`，待關閉該視窗後才可覆寫更新正式執行檔並就地重測。

## 2026-11-11 — 擴充「軍事部署電文」事件多樣性（8 種動態情境組合）
- **改善原因**：原 `EventSystem.CheckDynamic` 第 11 項 `Tag = "軍事部署"` 僅有單一固定事件（`參謀本部聯合作戰檢討會議`），玩家每次收到 `軍事部署電文` 皆出現相同選項。
- **實作內容（`EventSystem.cs`、`Program.cs`、`DESIGN.md`）**：
  - 新增 `EventSystem.CreateMilitaryDeploymentEvent` 與 `PickForwardIsland` / `SpendTreasury` 輔助函式，提供 **8 種涵蓋 2 卡（VS）與 3 卡策略抉擇之軍事部署事件組合**（聯合兩棲操演、我方外島要塞輪防、跨海登陸船團調度、東南沿海夜偵攔截、敵後蛙人突擊滲透、大陸光復區前進補給走廊、霧夜秘密搶灘運補、三軍機械化反登陸暨後備動員），自動帶入目前守備外島或光復省名，並以 `mil_tpl_*` 36 個月冷卻優先輪替不重複模板（全域 `gen_petition` 冷卻調整為 8 個月、機率 `0.30`）。
  - `--test` 驗證連續觸發 8 次產生 8 種不同事件（同時涵蓋 2 卡與 3 卡組合）、符合無禁止字樣規範且全數結算無誤。

## 2026-11-12 10:23 — Codex 接手開發交接現況（暫停工作區原始碼異動）
- **交接原則**：依使用者指示「`codex進入開發`」，Antigravity 停止對 `src\*.cs`、`DESIGN.md`、`RocGame.csproj` 與 `bin\` 之任何寫入／編譯異動，保留既有工作區修改供 Codex 繼續開發。
- **工作區相較於 `F:\DOC\misc\roc-grand-strategy-2d-game-20261110`（`2026-11-11 19:10` 歸檔版）的未歸檔差異**：
  1. `src\SimEngine.cs`（`2026-11-11 19:29:02`，`100,228 bytes`）：新增 `AttackNow`（第 683–768 行）與 `MoveNow`（第 771–816 行）即時調防／即時交戰邏輯。
  2. `src\MainForm.cs`（`2026-11-11 20:09:28`，`254,387 bytes`）：移除 `_pickTargetMode` 與 `_btnPickTarget`（「`左鍵點圖派兵`」），改為左鍵拖曳派兵（`_leftDragSource`、`_leftDragCommand`）與 `DispatchCheckedUnitsTo` 即時呼叫 `AttackNow`／`MoveNow`；同時微調科技頁籤字串（移除括號，保留 `★`/`◷` 徽章）、頁首摘要與卡片亮度字級。
  3. `DESIGN.md`（`2026-11-11 19:27:36`，`35,091 bytes`）：與歸檔版（`35,302 bytes`）略有差異（未在本次實作變更，交由 Codex 統一確認更新）。
  4. 其餘檔案（`EventSystem.cs`、`GameData.cs`、`ChinaMapGeo.cs`、`WarUiControls.cs`、`Program.cs`、`RocGame.csproj`、`assets\`）與 `19:10` 歸檔版一致。
- **最新編譯與實測狀態（2026-11-12 10:21）**：
  - 已確認無執行中遊戲鎖定，並完成正式建置：`dotnet build "c:\Users\typa3\agent_tmp\roc\RocGame.csproj" -c Release --no-restore`（0 警告、0 錯誤），產出 `bin\Release\net8.0-windows\國光：反攻大陸.exe`（`142,848 bytes`，`2026/11/12 10:21:11`）。
  - 執行 `國光：反攻大陸.exe --test` 於 `2026/11/12 10:21:15` 回傳 `exit code 1`，失敗於 `src\Program.cs:292`：`測試失敗: 左下指戰卡應具備『左鍵點圖派兵』切換按鈕`（根因：`Program.cs` 仍停留在 `19:10:31` 舊測試斷言，檢測已被 `MainForm.cs` 移除的「`左鍵點圖派兵`」按鈕與第 294 行「`✕ 取消點圖`」、第 296 行延遲待命 `Orders.Count == 3`）。
  - 測試中斷時於 `bin\Release\net8.0-windows\` 留有 `test_report.txt`（`450 bytes`）與 `ui_1950_snapshot.png`（`459,216 bytes`），待 `--test` 對齊後清理；`F:\DOC\misc\roc-grand-strategy-2d-game-20261110` 暫維持 `19:10` 最後通過 `--test` 之完整歸檔，待 Codex 開發完成並通過 `--test` 後再同步。

## 2026-11-12 11:55 — 戰線動態事件調整、禁止遊戲途中提早結局與終戰／全滅結算規則
- **Codex 協同基礎（33 節點／26 省真實行政區地圖＋即時拖曳調防）**：沿用 Codex 完成之 `ChinaMapGeo.cs`／`GameData.cs`（26 個大陸省分 ＋ 7 個海島 = 33 節點）、`MainForm.cs` 左鍵拖曳即時調防（`MoveNow`／`AttackNow`），並一併修正 `MainForm.cs`（第 1501、4094、5148 行）與 `Program.cs`（第 70 行）殘留的舊版 `/ 15` 省分母顯示為動態 `GameData.MainlandIds.Length`（26 省）。
- **事件全面隨戰線動態調整（`EventSystem.cs`）**：
  - 新增 `FrontlineTheater`（`StraitStandoff`、`SouthEastCoast`、`YangtzeCentral`、`NorthChinaPlain`、`NorthernBorderAntiUssr`）、`FrontlineContext`、`AnalyzeFrontline` 與 `PickFromPreferredOrFront`。
  - `HistoricalEvents`（包含舟山撤退、韓戰、大陳撤退、八二三砲戰、大躍進、伊塔事件、羅布泊核試爆、越戰、文革、珍寶島、天安門與 1996 飛彈危機）加入前線控制與非 ROC 目標保護，避免事件針對已光復省分誤扣守軍或出現過時地名。
  - `CheckDynamic`（第 1～13 項，新增第 13 項戰區專屬動態會戰 `dyn_theater_*`）與 `CreateMilitaryDeploymentEvent`（擴充至 12 種模板）全數接入 `ctx`：北推至華北／黃渤海後主動轉為山東／河北／山西／黃渤海敘事並停止觸動福建；僅剩北疆俄占區時自動切換為「俄軍／蘇俄軍」與北疆塞外抗俄語境，不再出現「東南沿海」或「共軍」錯置；並移除 `dyn_warlord_crisis` 與 `dyn_roc_warlord` 選項內直接呼叫 `SimEngine.Finish` 的行為。
- **結局改為「僅時間結束後出現，或被全滅時出現」（`SimEngine.cs` `CheckEnd`、`EventSystem.cs`、`Program.cs`）**：
  - `SimEngine.CheckEnd` 於 `s.Year <= GameData.EndYear` 期間僅在 `rocWipedOut`（我方全境失守，或台灣失守且無大陸據點及存活地面部隊 → 立即 `invaded`）或 `allMainlandRoc`（大陸 26 省全數由國軍收復、敵方全數肅清全滅 → 立即 `full`）時立即終局。
  - `warlordP`、`warlordR`、`mutiny`、`collapse`、`bankrupt`、`armistice`、`acceptUssrSplit -> split_ussr`、`annexed`，以及僅克復河北但仍有未收復省分之情形，一律延後至 `s.Year > GameData.EndYear` 終戰後方結算（`dyn_armistice_offer`、`beijing_ussr_choice`、`dyn_ultimatum` 改為記錄外交妥協立場與喘息效果並繼續戰局）。
- **G3 實機與自動化驗證**：
  - `dotnet build "c:\Users\typa3\agent_tmp\roc\RocGame.csproj" -c Release --no-restore`：0 警告、0 錯誤。
  - `國光：反攻大陸.exe --test`：`TEST_EXIT_CODE=0`，全數 1、2（含 `2B` 華北／北疆戰線驗證）、3、4（含遊戲中不搶跑結局、僅時間結束或全滅終局與全動態事件選項無過早結局迴歸）、4B、5 全部 `[PASS]`；已目視檢查 `ui_snapshot.png`（頂部 HUD 正確顯示 `光復 1/26 · ⚠俄占1` 且字體清晰無裁切），並已清除 `bin\Release\net8.0-windows\` 內所有 `ui_*.png` 與 `test_report.txt` 暫存檔。

## 2026-11-12 — 試玩回饋「有點單調」：設計與任務切割（尚未改碼）

### 玩家原話
「我剛試玩發現有點單調」。隨後要求自行設計並切割任務。Gemini 3.8 本輪未產出設計（預設模型額度用盡；3.8 唯讀呼叫因 headless 指令權限被拒；後續呼叫被取消）。以下常數與呼叫點依已讀原始碼，不是實測戰局。

### 根因（程式證據，尚未用一局實玩計時確認手感）
1. `SimEngine.AdvanceDay` 只在每月 15 日呼叫 `EventSystem.CheckDynamic`，且命中第一個事件就 return。1950 不登陸時 `PrcStab` 65、`Morale` 52、大陸光復 0，俄侵／逃兵／民兵／投誠多半不開；`TotalMonths < 2` 直接返回；海空巡弋要 1951、經建要 1952；軍事部署電文機率 0.30、冷卻 8 個月。多數月份沒有決策。
2. `PrcCounterattack` 要約 3:1 有效戰力，且來源省得留名義守軍 55%。外島登陸在 `PrcAi` 內機率 0.032，還要海空 1.4 倍且該島 `Garrison == 0`。島嶼 `BaseGarrison` 為 0，月結戰鬥迴圈在守軍為 0 時不打。玩家不登陸，省色不變。1× 約 240ms／日、約 87 秒一年。
3. 戰報在抽屜「戰報與歷史電文」。地圖頂部是固定的北方強鄰牌與圖例。
4. 1950 海空：Z3 兩支艦隊、Z2 一支；南海 Z4 開局沒有我方艦機。共軍海軍曲線 1950 為 6，乘海域權重（Z4 = 0.18）與穩定係數。南海／海南是開局暴露面。此為公式推估，尚未印出實數。

### 設計原則（2026-11-12 使用者補充：這款遊戲注重策略；事件需依遊戲進度調整）
先前的「每月自動削守軍／自動試探」撤回。那會替玩家打完仗，策略空間變少。

玩家已經用拖曳決定艦隊海域與部隊位置。單調來自這個部署長期沒有對應的取捨：海空擺一邊時，另一邊不會變成必須回答的問題；優勢海域也不會變成「現在打或讓共軍補上」的窗口。

新原則：每月只更新「態勢句」（哪個海域暴露、哪裡有窗口），不改數值。真正的策略卡低頻率出現，時間暫停，三選一，而且三個選項都移動現有部隊或接受現有後果。部署平衡時不彈卡，安靜是佈局正確的結果。

進度三軸，事件同時看這三軸，不另做進度條：
1. 戰線位置：沿用 `AnalyzeFrontline` 的 `FrontlineTheater`、光復省數、當前敵前節點與爭奪海域。
2. 年代：年份與現有 `PriceIndex`。國庫花費繼續乘物價。
3. 前提是否還在：目標省已光復、外島已放棄、該海域已無我方島嶼、或大陸已無該敵方時，這張卡不出現。史實日期事件維持現有 `Cond`。

同一種卡在不同階段問不同的事：
- 台海對峙（大陸光復 0）：外島與海空掩護。增援來源是台灣。
- 東南沿海：已佔灘頭的補給，以及下一個沿海敵省。增援來源改為後方我方省，台灣只作後備。
- 江淮：前方省的補給線，以及被拋在身後的海岸。窗口目標改為當前敵前省，不再寫福建。
- 華北：平原推進與黃渤海。若已有俄占省，第三選項改為北疆兵力，不再是「按兵」。
- 只剩北疆俄占：不出現渡海窗口。問的是邊境增兵、縮短防線，或既有的對俄施壓。

數值跟著目標現況，不用固定「-0.6 師／動盪 +10」：
- 破襲減少的守軍 = 該省當前 Garrison 的 8%–12%，地板仍是 `max(0.8, 名義守軍 × 0.45)`，省色不變。
- 補給消耗按實際調動的部隊數計算。
- 冷卻按戰區分開記。同一戰區 6 個月一張；戰區變了，新戰區可以問新的問題，不必等舊冷卻。

沿用限制：不加「快進至事件」，不新增 1996 年前結局，不加每省按鈕，不寫「史實／架空」。易手仍只走現有 `Battle()` 與「我方節點無地面部隊且 Garrison > 0 則失守」。海域代號不得當作 `Control`／`Garrison` 的鍵。調動必須走現有 `CheckMove`，運力不夠的選項不可用並寫明原因。

### 任務（依序）

**S1 態勢判斷，不改數值** — `SimEngine.cs`
- 新增 `FrontHeadline`、`FrontHeadlineKind`（`threat`／`window`／`quiet`）。舊存檔缺欄位視為空。
- 新增 `AssessPosture(s)`，於 `MonthlyTick` 末尾呼叫。對 Z1–Z4 比較 `RocSea+RocAir` 與 `PrcSea+PrcAir`。
- 暴露：共軍海空 ≥ 我方，且該海域有我方外島（不含台灣本島）。
- 窗口：我方海空 ≥ 對方 × 1.25，且該海域有共軍沿海省。
- 同時有暴露與窗口時，頭條只寫暴露。都沒有則寫對峙。
- 完成條件：`NewGame` 固定種子跑一個 `MonthlyTick` 後，頭條指出南海／海南暴露；大陸控制權與各單位 Str、各省 Garrison 與開局相同；`Over == null`。

**S2 地圖態勢句** — `MainForm.cs`
- 頭條非空時，在頂部兩張牌下方（約 y=36）畫一條置中銘牌。`threat` 用警示色，`window` 用正向色，`quiet` 用中性色。
- 完成條件：S1 那個月份能在地圖上讀到暴露句，且不壓到左右頂牌。

**S3 暴露卡，內容隨進度變** — `EventSystem.CheckDynamic`
- 僅在該月沒有其他事件、存在暴露目標、且該戰區冷卻已過時出現。時間暫停。
- 文案、島嶼、海域、增援來源都從當下的 `AssessPosture` 與 `AnalyzeFrontline` 取。前提消失就不發這張卡。
- 台海對峙的三選一仍是：增援暴露外島、海空改駐暴露海域、把島上部隊收回到當時的後方。
- 東南沿海以後，暴露目標改為當前灘頭側翼或身後失去掩護的海岸，增援來自後方我方省。
- 只剩俄占區時，三選一改為邊境增兵、縮短防線、對俄施壓，不出現外島與艦隊。
- 選項一律呼叫 `CheckMove`。運力不夠的選項不執行，提示寫缺什麼。
- 完成條件有兩組快照：
  1. 1950 開局：卡寫南海／海南，增援來自台灣。改駐後 Z4 有我方艦或機。收縮後該島仍為 ROC、Garrison 為 0。
  2. 已光復閩粵、戰線在山東的存檔：卡不得再以海南或「從台灣增援海南」為主題，目標改為當前暴露面。大陳若已不是 ROC，不得再生成大陳卡。
- 全程 `Over == null`，這張卡本身不改大陸省色。

**S4 窗口卡，目標與力道隨進度變** — `EventSystem.CheckDynamic`
- 僅在沒有暴露、我方有窗口、該月沒有其他事件、該戰區另一組冷卻已過時出現。暴露卡優先。
- 目標省取該優勢海域上的當前敵前省。戰線到江淮或華北時，不得再把福建寫成主目標。只剩俄占區時不發渡海窗口。
- 選項：
  1. 破襲：補給按參戰部隊計算；目標省 Garrison 減少其當前值的 8%–12%，不低於地板，省色不變。該海域獲得一個月登陸窗口標記。
  2. 封鎖：該省動盪上升，一支參與艦隊 Str 小幅下降。華北且已有俄占時，此項改為抽部隊加強北疆。
  3. 按兵：不改 Garrison。頭條寫明共軍將依現有增援把窗口補上。
- 完成條件：1950 補上 Z4 艦隊後，窗口目標是南海沿岸敵省。另一個「閩粵已光復、優勢在黃渤海」的快照，窗口目標是山東或河北，破襲用百分比，省色仍為 PRC。守軍 4 師與守軍 20 師被削掉的絕對師數不同，百分比同階。

**S5 既有動態事件的力道一併隨進度** — `EventSystem.cs`
- `CreateMilitaryDeploymentEvent` 與 `CheckDynamic` 裡仍寫死「守軍 -0.6 師、動盪 +10、補給 -4」的效果，改成對當前敵前省的百分比，並乘現有物價。地名已隨戰線變化的維持原樣。
- 全國性史實事件（大躍進、文革這類）的共方懲罰按「仍屬中共的大陸省比例」縮放。點名省分的史實事件繼續只用現有 `Cond`，前提不在就跳過。
- 完成條件：同一張巡弋卡在守軍 4 與守軍 20 上扣掉的師數不同；福建已光復時，八二三砲戰不出現；大躍進在只剩少數中共省時，穩定度降幅小於全大陸仍為中共時。

### 建議執行順序
S1 → S2 → S3（含 1950 與北進兩組快照）→ S4 → S5 → 再試玩。

## 2026-11-12 — 補後期時局事件（1973–1995 空窗）

使用者要求「添加更多的事件，不然玩到後面有點單調」。1972 之後原先只在 1978、1979、1982、1987、1989、1991、1996 有必出事件，中間常空 3–5 年。

### 新增 10 張，日期一到就進佇列（`src/EventSystem.Late.cs`）
`paris_1973`、`oil_1974`、`chiang_1975`、`mao_1976`、`reagan_1981`、`hk_1984`、`ccg_1988`、`gulf_1990`、`wangku_1993`、`cornell_1995`。每張 3 選 1。觸發時 `Specialize` 把文案裡的前線換成 `AnalyzeFrontline` 的當前戰區與敵前省名。大陸已無中共省時，依賴對岸前提的五張（巴黎協定、權力真空、香港、兩岸接觸、出訪）不出現。

力道：施壓類削當前敵前守軍的 8%–12%，地板 `max(0.8, 名義守軍 × 0.45)`，不因此改省色。穩定度懲罰乘上「仍屬中共的大陸省比例」。出訪的代價是敵前守軍 +8%，不是加分券。

S1–S4 的態勢句與暴露卡尚未做。

### 驗證
`dotnet build -c Release`：0 警告、0 錯誤。`國光：反攻大陸.exe --test`：`test_report.txt` 為全部通過（含 4C：對峙時權力真空指向廣東／廣西，閩粵浙蘇光復後改指向山東；守軍 20 削得多於守軍 10；十張事件各選項不在中途結束、台灣仍為 ROC）。

### 撤回
每月自動削戰力、無玩家選擇的共軍試探、以扣血製造「地圖在動」、以及任何寫死福建／海南而不看當前戰線的新事件，不實作。

## 2026-11-12 — 終戰年改到 2025、科研加快、1999–2024 事件

使用者兩句：科研時間還是太久，要評估合適的時間；不須限制遊戲結束年分，持續到 2025 也可以，並補上俄烏戰爭等真實與虛構事件。

### 終戰年
`GameData.EndYear` 由 1996 改為 2025。上限是往後移。`CheckEnd` 在這一年之前仍只在全滅或大陸 26 省全數光復時立即結束。停戰、破產、軍閥、北疆俄占等結局的程式條件是 `Year > EndYear`，所以保留一個終戰年，這些結局才結算得到。開局副標改為「1950–2025」。1× 全程約 111 分鐘，2× 約 55 分鐘。

國力曲線接到 2025，否則 1997 以後會停在 1996 的數字：中共 GDP 182000、台灣 GDP 8150、共軍海空與飛彈、物價指數 58。單位仍是億美元。`DESIGN.md` 結局表的終戰年同步改成 2025。維持現狀結局的 GDP > 1800 門檻沒有改。

### 科研
`SimEngine.RpPerTurnFor` 的常數由 18 改為 32。1950 開局、研究等級 1、GDP 14 時，年研發點數約 32.23。`EstDaysRemaining`：外島雷達（Cost 4）46 天，核武原型（Cost 28）318 天。改之前同一組約 81 天與 561 天。專案趕工仍是速度 +50%、成本 +40%，Cost 4 約 31 天、Cost 28 約 212 天。科技年份門檻（裝甲 1955、兩棲準則 1960 等）與各項 Cost 沒有改。1× 下 Cost 4 約 11 秒、Cost 28 約 76 秒；4× 下約 2.3 秒與 16 秒。

### 新事件
`src/EventSystem.Late.cs` 再加 14 張三選一，標籤仍是「時局」，不寫「史實／架空」，效果不呼叫終局：
`liangguo_1999`、`election_2000`、`wto_2002`、`antisec_2005`、`links_2008`、`diaoyu_2012`、`sunflower_2014`、`scs_2016`、`hk_2019`、`covid_2020`、`ukraine_2022`、`pelosi_2022`、`drone_2023`、`budget_2024`。

施壓仍削當前敵前守軍的百分之幾，地板 `max(0.8, 名義守軍 × 0.45)`，不改省色。艦隊調動只寫海域 `Loc`（東海 Z2、南海 Z4），不拿海域代號查 `Control`／`Garrison`。

`ukraine_2022`（2022-02-24）與 `drone_2023`：北疆有俄占省時，文案點出那些省名，第三選項削俄占省守軍（15%／8%）且控制方不變。沒有俄占省時，歐洲戰爭改削當前敵前約 8%，文案不寫新疆被占。2022 年情報電文 `era_2022` 在 `SampleIntelReports`。

### 驗證
`dotnet build -c Release --no-restore`：0 警告、0 錯誤。`國光：反攻大陸.exe --test` 退出碼 0。`test_report.txt` 為 PASS 1、2、3、4、4B、5，以及「全部自動化測試 100% 通過」。4C 在 PASS 4 之內，含終戰年 2025、2025 曲線高於 1996、科研 40–55 天與 280–340 天、新疆設為 USSR 時守軍下降且控制方仍是 USSR、無俄占時不寫新疆且敵前守軍下降省色不變、十四張新事件每個選項 `Over == null` 且台灣仍為 ROC。

沒有從 1950 手動玩到 2022 看事件卡畫面。UI 自動化只推進約一年。Gemini 未呼叫。交叉審查未跑。S1–S5 仍未做。

## 2026-11-12 — 共軍部署隨年份變厚，並顯示在地圖與頂欄

使用者遊玩時沒看到敵軍隨時間增強。

### 原因
名目守軍是 `Base × (1 + (年-1950) / 110)`，一年大約 +0.9%。福建開局 11 師，打五年大約 11.5。整補又按名單順序先灌給黑龍江，玩家盯的福建、廣東更慢。海空曲線其實在漲，但海域條只寫制海／制空百分比，絕對值不出現。綜合軍力只在民心條的懸停說明裡。

### 改動
- `CurvePrcGarrison`：1950 為 1 倍，1952 為 1.20，1956 為 1.45，1962 為 1.80，1996 為 3.30，2025 為 4.45。福建名目師數約 11 → 13.2 → 16 → 20 → 36 → 49。
- 每月整補池由每年約 3.5 師改為約 10 師起跳，並按各省缺口比例分，不再整筆給名單第一省。
- 頂欄右側改為「共軍 N 師」。部署每增加約 3 師，戰報寫一次「共軍持續擴編」。
- 海域條改為「海 nn% 共X」「空 nn% 共Y」。陸軍科技曲線仍另外提高每一師的戰鬥力，沒有再乘進師數，避免算兩次。

### 驗證
Release 建置 0 警告 0 錯誤。`--test` 退出碼 0，報告為全部通過。其中 1951–1952 逐月結算後，福建至少 +1.5 師、廣東至少 +0.5 師、全大陸部署至少 +10 師，控制方不變。沒有手動從 1950 看到 1962 的畫面。

## 2026-11-12 — 交戰省分左右各半上色

使用者要求交戰區用顏色各半顯示。

省境上同時有國軍地面部隊與敵軍守備（`SimEngine.IsContested`）時，`FillContestedPolygon` 把該省分成左右兩半：左半是目前控制方，右半是另一方。守軍清零或國軍不在場上就回到單色。據點圓點同樣左右各半。俄占斜紋在交戰時先讓出來，避免蓋住分色。圖例寫「左右各半＝交戰」。

開局福建不是交戰區。測試把步兵放進仍有守軍的廣東，判定為交戰；守軍歸零後取消。矩形填色抽樣：左側偏藍、右側偏紅。`--test` 全部通過。沒有在實機畫面上點開一場進行中的戰鬥來看整張地圖。

## 2026-11-12 — 後期膠著出現聯合國會談

使用者要求：後期戰事持續膠著超過一定時間，就開始出現和談聲浪。

`FrontSig` / `FrontStableSince` 記錄上一次省分易手。1972 年起，連續 36 個月沒有省分易手，而且雙方都還在，每月 15 日的動態事件會先出聯合國卡。之後每 24 個月再出一次。中間只要有省分易手，計時歸零。36 個月是會談聲浪，60 個月是停火觀察，96 個月是強制調解。文案用當前戰線，台海膠著寫廣東／廣西，閩粵浙蘇已光復則改寫山東。

三個選項都不在終戰前結束遊戲。接受會談記下 `armistice`，攻勢加成下降；拒絕則削減當前敵前約 10% 守軍且不改省色；旁聽只拿一點外交進項並輕微施壓。終戰年之後，這份停火才結算成停戰結局。

`--test` 全部通過：1969 年不出現；1975 年未累積膠著不出現；易手後消失；接受會談在 1976 年 `Over == null`，撥到終戰年才是 `armistice`；拒絕不改省色。沒有手動把一局放到 1975 年看這張卡。


## 2026-11-12 — 接手完成「戰地電訊」與「停火滿三年中國分治」未編譯階段

檢查同日 `14:58–14:59` 另一工作階段留下的未編譯修改，並補齊修繕與驗證。

### 發現之未完成項目
- `src\EventSystem.cs`：`FrontlineContext.FrontNotice` / `EventSystem.FrontNotice`（依敵前戰線與俄軍情勢輪播戰地彈藥／投降／偵察等前線電訊）；`dyn_un_*` 接受和談選項改寫 `st.Flags["ceasefireAt"] = st.TotalMonths + 1`（「停火滿三年後結束遊戲，中國沿當時戰線分治」）。
- `src\SimEngine.cs`：新增 `CeasefireMonths = 36`、`CeasefireMatured(GameState)`、`FinishPartition(GameState)`（標題「中國分治・停火線確立」、分類「停止戰線」，列明雙方與俄占當時實控省分），並於 `CheckEnd` 優先檢查 `CeasefireMatured(s)`。
- `src\MainForm.cs`：頂欄 `_hudPanel` 由 `56px` 增為 `78px`，於下方加入「戰地電訊」條（`_noticeText` / `PulseFrontNotice()` / `FrontNoticeTextForTest`），且提示卡加入 `共軍部署` 顯示。但停在第 1519 行寫錯列舉型別 `StringFormat.EllipsisCharacter`（應為 `StringTrimming.EllipsisCharacter`），尚未通過編譯。
- `src\Program.cs`、`DESIGN.md`：已補入 `ceasefireAt` 停火滿三年分治、`FrontNotice` 測試斷言與 `DESIGN.md`（1950–2025、`戰地電訊`）條目；唯 `DESIGN.md` 結局表第 89 行仍舊寫 2025 年終戰、目錄樹未列 `EventSystem.Late.cs`，一併補齊。

### 修正與驗證
- 修正 `src\MainForm.cs:1519` 為 `Trimming = StringTrimming.EllipsisCharacter`。
- 同步補齊 `DESIGN.md`（停止戰線觸發條件與 `src\EventSystem.Late.cs` 架構說明）與 `F:\DOC\misc\INDEX.md`。
- 執行 `dotnet build -c Release --no-restore`：0 警告、0 錯誤。
- 執行 `國光：反攻大陸.exe --test`：退出碼 `0`，6 項自動化測試組（含 `ceasefireAt` 第 35 月未結算 / 第 36 月分治結算、`FrontNotice` 前線輪播）全數通過。
- 檢視 `ui_snapshot.png`、`ui_1950_snapshot.png`、`ui_event_snapshot.png`、`ui_zoom_snapshot.png`：確認 `78px` 頂欄內「戰地電訊」完整顯示、不與上方「北方強鄰」橫幅／右側圖例重疊；交戰省分左右各半填色與共軍總師數、海域顯示均正常。

### 2026-11-12 後續調整 — 拿掉戰報抽屜內「結局圖鑑」分頁
- 移除 `src\MainForm.cs` 戰報抽屜之「結局圖鑑」分頁按鈕、`else` 結局卡片渲染與 `_intelSubTab` 狀態欄位；`RenderIntelDrawer` 直接顯示「戰報與歷史電文（`State.Logs`）」。
- 底部按鈕由「`★ 戰報結局`」改名為「`★ 戰報日誌`」，抽屜標題改為「`★ 統帥部戰報與歷史電文`」；對局結束時之 `ShowEndingOverlay` 結算視窗維持不變。
- 同步更新 `DESIGN.md`。

### 2026-11-12 後續調整 — 生兵系統簡化（4 大可造主力＋大陸港口就地整編）
- `src\GameData.cs`：保留 `mar`、`abn`、`amph`、`mil`、`dfc` 定義以相容開局／事件／存檔，並將 `mar`、`abn`、`amph` 標記 `NoBuild = true`；建軍面板僅保留 **4 大可造主力**（`步兵師 inf`、`裝甲突擊師 arm`、`艦隊 fleet`、`航空聯隊 air`）。研發「空降特戰兵團」改為直接於本島編入 1 支空降旅並附加 `+5%` 作戰加成。
- `src\SimEngine.cs`：`LiftCapacity` 讓每支 `艦隊` 額外提供 `+1.0` 師跨海運力（免再單獨造兩棲船團）；新增 `IsBuildBase` / `BuildBases`，並將 `CanBuild` / `BuildUnit` 擴充為支援於「台灣本島」或「已穩定控制且非交戰中之大陸港口省分」就地編成地面主力。
- `src\MainForm.cs`：`RenderBuildDrawer` 精簡為基地選擇鈕（當控制大陸港口時顯示）＋ 4 張主力卡片；省分指戰卡（`_nodeCard`）於台灣及大陸港口省分右側加入 `編成步兵` 快捷鈕。
- `src\Program.cs`、`DESIGN.md`：補入 4 主力與大陸港口就地生兵斷言及設計文件同步。

### 2026-11-12 後續調整 — 六邊形向量圖標科技樹＋懸停詳情浮卡＋點擊扣國庫
- `src\SimEngine.cs`：新增 `TechStartCost(s, t)`（`Round((0.05 + Cost * 0.015) * PriceIndex, 2)`）；`ClickTechAction` 點擊啟動／改研／排程科技時優先檢查並扣除國庫（旗標 `techPaid_<id>` 防止切換重扣，國庫不足回傳 `"no_funds"`），取消尚未動工的排程時全額退還預撥經費。
- `src\MainForm.cs`：將科技樹從大型文字方塊全面改為單螢幕全覽之**六邊形向量圖標樹**（`TechHexPoints`、`DrawTechProgressHexRing`、`DrawTechVectorIcon` 為全部 40 項科技配製 18 種鮮明識別圖標），節點下方僅留精簡名稱與金額／天數；滑鼠移上六邊形即於旁側繪出 `DrawTechHoverTooltip` 浮動詳細說明卡。
- `src\Program.cs`、`DESIGN.md`：補齊扣款／國庫不足擋下／退排程退款測試斷言，並更新 `ui_tech_snapshot.png` 與 `ui_tech_science_snapshot.png` 懸停實機渲染檢視。

### 2026-11-12 後續修正 — 軍情處頁面自動暫停時間＋既有機制全面核對
- **根因確認**：`IsClockRunning`、`SetGameMode`、`TogglePause`、`SetSimSpeed`、`UpdateSpeedButtonsUi`、`ProcessCmdKey`、`HudPanel_Paint`、`UpdateOverlays`、`StepClockTick` 及 `OpenIntelPage` / `CloseIntelPage` 原僅針對 `_techPageOpen` 停表，漏掉 `_intelPageOpen`。
- **修繕（`src\MainForm.cs`）**：抽出統一屬性 `IsFullOverlayOpen => _techPageOpen || _intelPageOpen`，將前述全數停表／鎖速／暫緩事件／空白鍵／HUD 狀態（「`▌▌ 軍情暫停`」）同步涵蓋軍情處全版面專頁，關閉軍情處時再呼叫 `UpdateOverlays()`、`UpdateSpeedButtonsUi()`、`SyncSimTimer()` 恢復原流速。
- **迴歸檢查（`src\Program.cs`、`DESIGN.md`）**：於 `Program.cs` 補入開啟軍情處停表、日期凍結、關閉恢復流動斷言，並重新核對全部既有核心功能（戰地電訊、停火分治、四大主力＋大陸港口生兵、六邊形向量圖標科技樹＋立項扣國庫、戰報日誌與結局結算）。

### 2026-11-12 後續實作 — 短程真實行軍小圖示＋兩棲籌備/制海≥50%/奪港＋前線自動補防（非 HOI4 化）
- **設計決策**：依使用者確認不走 *Hearts of Iron IV* 繁瑣戰線畫筆模式，保留直覺左鍵拖曳／右鍵指派手感，將原本瞬間傳送之 `MoveNow` / `AttackNow` 改為真實每日推進之行軍與兩棲登陸籌備機制。
- **`src\SimEngine.cs`**：
  - 擴充 `MoveOrder`（`Path`、`TotalDays`、`ElapsedDays`、`IsPrep`、`PrepDays`、`PrepRequired`、`ManualOverride`、`FromAutoFront`）與 `GameState`（`AutoFrontFill`、`PriorityTargets`）。
  - 新增 `FindFriendlyLandPath`（後方友軍控制區跨多省最短安全尋路）、`ComputeTravelDays`（空降 2 天、相鄰陸路 2～5 天、多省陸路封頂 8 天、港島跨海 3～5 天）、`PrepRequiredDays`（兩棲籌備 4～8 天）與每日 `StepOrders`（籌備累積＋沿線制海 `>= 50%` 檢定通過後自動起航，並於抵達當日結算交戰或調防）。
  - 強化 `ResolveSupply` 連回己方港口走廊判定（凸顯搶下沿海港口之補給價值）、強化 `AutoBalanceNavalAir` 對待命登陸海域加權，並將 `AutoBalanceLandForces` 升級為每 3 天視需要將非前線後方餘裕部隊真實行軍派往邊界前線與 `PriorityTargets`（台灣保留守備且不搶手動部隊）。
- **`src\MainForm.cs`**：
  - 將路線繪製拆分為底層 `DrawMovementLines` 與節點上層 `DrawOrderBadgesAndTokens`（搭配 `DrawMarchingToken` 繪出步兵／裝甲／登陸艦／空降迷你小圖示＋師數＋進度條＋剩餘天數，以及兩棲 `籌備 X/Y 天 · 海Z%✓ · 港` 狀態牌，並支援懸停提示與右鍵撤令）。
  - 右下角原本靜態「自動調兵佈防」改為可切換之 `⛨ 自動補防：開/關`；指戰卡右上角加入 `☆ 重點` 星標按鈕並呈現行軍/籌備中與在途增援數。
- **`src\Program.cs`、`DESIGN.md`、`TROOP_MOVEMENT_BENCHMARK.md`**：補入 `Test 4C`（`< 50%` 制海擋航、達標起航、跨省尋路與前線自動補防）及在途截圖實機驗證；同步更新 `DESIGN.md` §七.3。

### 2026-11-12 18:38 — 實作 1950 死守與增兵海南四重困境（對齊歷史戰略與「添油戰術」風險）
- **問題定位**：原先 `PrcAi` 將外島登陸鎖在 `1951+`、攻台鎖在 `1952+`，`CheckMoveDetailed` 讓未直接接戰的港島間可無限免費跨海調兵，`ResolveSupply` 預設所有外島永遠 `100%` 補給暢通且需求減半，導致玩家在 1950 年即使把台灣、舟山主力全數調往死守海南島，也感受不到兵力空虛、千公里運力封鎖、四野瓊州海峽無限壓境與「添油戰術」的風險。
- **實作修改**：
  - `src\SimEngine.cs`：
    - 海南開局下調為粵桂殘編 `65%` 戰力並設 `hiQiongya = 1`（`RocPowerAt × 0.80` 反映派系失調與瓊崖縱隊內應，收復廣東後解除）。
    - 新增 `NeedsContestedLanding`，於海口封鎖（`hiPortBlocked`）或海南交戰時強制增援走兩棲籌備與 `1.5×` 遠距逆登陸運力消耗，且 `StepOrders` 抵達時施加砲火下逆登陸戰損與警訊。
    - `ResolveSupply` 對海南（`HI`）改採逾千公里遠距消耗係數（屯兵或圍困時 `1.05×～1.35×`）並依南海 `Z4` 制海與港口封鎖狀況折減送補率。
    - `PrcAi` 實作四野背靠華南跨過 20–30 公里瓊州海峽持續增兵 `HI`，並於 `!UsProtect` 時期偵測舟山或台灣主力被抽空（`twDef < 5.8`）立即拉響「本島極危」與三野 `1950` 渡海進犯風險；`SampleIntelReports` 同步補充預警。
  - `src\EventSystem.cs`：`zhoushan_1950` 選擇死守海南（「南北並守」）時啟動 `holdHainan`、`hiPortBlocked`、`hiQiongya` 與 `4.2` 師四野登陸重壓；撤退則乾淨清除對應旗標；`OnCapture("GD")` 解除封鎖與瓊崖威脅。
  - `src\MainForm.cs`：選取海南島時於省分指戰卡顯示 `⚠海口封鎖(砲火逆登陸)` 或 `⚠瓊崖內應`。
  - `src\Program.cs`、`DESIGN.md`：補入自動化測試 `5B`（驗證 1950 開局殘編／瓊崖內應、死守觸發 `1.5×` 逆登陸運力、低制海下補給惡化 `SupFactor < 0.85`、瓊州海峽續增與抽空本島 `twEmptyWarned`）並更新 `DESIGN.md` §八。
- **驗證狀態（Rule G5）**：
  - `dotnet build -c Release`：0 警告、0 錯誤（`EXE_NEWER=True`）。
  - `國光：反攻大陸.exe --test`：同步執行退出碼 `0`，`[PASS] 1 / 2 / 3 / 4 / 4B / 5 / 5B` 全部通過；已清除 `test_report.txt` 與 `ui_*.png`。
  - 未另外開視窗人工點擊遊玩多月，但所有數值與旗標轉換均已由 `--test` 實機驗證。

### 2026-11-12 18:58 — 移除各軍事單位籌碼旁的 `-` / `+` 小按鍵
- **修改（`src\MainForm.cs` `BuildVisualUnitChips`）**：移除每張待命單位籌碼右側原本的 `btnMinus` (`-`) 與 `btnPlus` (`+`) 小按鈕；改將文字欄拉寬置中顯示（單一單位已選時附加 `✓`），並讓整張籌碼框（`chip.Click` 與 `lbl.Click`）皆可直接點擊切換全選／取消（數量微調繼續由上方 `1師 / 3師 / 半數 / 全選 / 撤令` 控制）。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release`：0 警告、0 錯誤（`EXE_NEWER=True`）。
  - `國光：反攻大陸.exe --test`：退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B` 全部 `[PASS]`。
  - 已實際讀取 `ui_1950_snapshot.png` 與 `ui_snapshot.png` 確認各兵種籌碼旁無任何 `-` / `+` 按鍵且置中清晰，隨後已清除暫存圖與 `test_report.txt`。

### 2026-11-12 19:13 — 取消科技研發排程、改為撥款即並行研發並大幅縮短研發時間
- **問題定位**：玩家反映科技已須撥付國庫啟動經費且具年代／前置門檻，不應再強制單線 `ResearchQueue` 排程等待，且原有研發速度仍偏久。
- **實作修改**：
  - `src\SimEngine.cs`：
    - `GameState` 新增 `ActiveTechs` 並行研發清單（`Normalize` 兼容舊存檔將原 `ResearchId` 及已撥款排程遷移進 `ActiveTechs` 並清空 `ResearchQueue`）；新增 `HasActiveResearch`、`IsResearching`、`SyncPrimaryResearch`。
    - 簡化 `ClickTechAction(s, id)`：移除排程入列、取消排程與右鍵強制切換邏輯，凡符合前置／年代且國庫足夠立項經費者，點擊立即撥款加入 `ActiveTechs` 開始推進；`StepResearch` 同步按天推進所有 `ActiveTechs` 並同步事件增量至 `ResearchProgMap`。
    - 大幅加速 `RpPerTurnFor`：`s.ResLevel * (96.0 + s.Gdp / 30.0) + s.RpBonus * 6.0`（1950 Lv.1 GDP 14 下，`Cost 4` 外島雷達由 `46 天` 降至 `16 天`、`Cost 5` 耕者有其田 `19 天`、`Cost 28` 核武原型由 `318 天` 降至 `106 天`，開啟趕工 `+50%` 速度再縮減三分之一）。
  - `src\MainForm.cs`：移除科技頁首「排程 N」、分頁 `◷`、六邊形 `#2` 徽章及右鍵切換提示；所有 `ActiveTechs` 分頁皆亮起 `★` 且節點皆繪製金色進度六角環與實際剩餘天數（改以 `SavedResearchProg(State, t.Id)` 各自讀取進度）。
  - `src\Program.cs`、`DESIGN.md`：更新天數區間斷言（`dCoast` `10–22 天`、`dNuke` `80–125 天`），改寫科技樹測試驗證多分支同時撥款並行研發與短天數同時完成；同步更新 `DESIGN.md` §十.3。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤（`EXE=2026-11-12T19:12:26` 與 DLL 一致）。
  - `國光：反攻大陸.exe --test`：退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B` 全部 `[PASS]`。
  - 已實際讀取 `ui_tech_snapshot.png` 與 `ui_tech_science_snapshot.png` 確認排程字樣與 `◷` 全數消失、並行進度條與天數清晰可讀，並已清理暫存圖與 `test_report.txt`。

### 2026-11-12 19:26 — 查看科技樹時不暫停時間・發生事件時才自動暫停
- **修改（`src\MainForm.cs`）**：
  - 將原先綁定兩大專頁的 `IsFullOverlayOpen` 改為 `IsOverlayPausingTime => _intelPageOpen`，使**查看科技樹期間時間依現行倍速持續流動**（允許透過右上按鈕或空白鍵切換倍速與手動暫停，`StepClockTick` 逐日即時重繪研發進度與剩餘天數；標題欄徽章改顯示 `▶ N× 時間流動中 · 事件發生時自動暫停`）。
  - `UpdateOverlays` 改為 `else if (State.PendingEvents.Count > 0 && !_intelPageOpen)`：於科技樹頁面期間一旦觸發事件，時間立即自動暫停（`!IsClockRunning`）並直接彈出事件卡於最上層，待玩家處置完所有待決事件後不須離開科技樹即自動恢復時間流動。
  - 確認原有功能保留：`◎ 軍情處` 全版面專頁依然維持開啟即自動暫停時間（`▌▌ 軍情暫停`）與日期凍結。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤（`EXE=2026-11-12T19:25:35` 與 DLL 一致）。
  - 更新 `src\Program.cs` 並執行 `國光：反攻大陸.exe --test` 退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B` 全部 `[PASS]`（已實測開啟科技樹時間續流推進日期與研發點、發生事件立即暫停並彈出事件卡、處置事件後自動恢復流動，以及軍情處仍保持暫停時間）。
  - 已檢視 `ui_tech_science_snapshot.png`（顯示 `1967年01月03日 ▶ 1×`、`▶ 1× 時間流動中 · 事件發生時自動暫停` 與即時累加進度 `10.94/20`）及 `ui_intel_page_snapshot.png`（顯示 `▌▌ 軍情暫停`），隨後已清除暫存圖與 `test_report.txt`。

### 2026-11-12 22:17 — 修復 1950 韓戰日期錯置並重構多元動態事件加權輪抽池
- **根因定位（G1）**：
  1. 韓戰事件原誤植為 `korean_war_1951`（`Year = 1951, Month = 2, Day = 15`），導致 `1950/04/10` 後整個 `1950 年 6–12 月` 遲不觸發韓戰與第七艦隊協防。
  2. `CheckDynamic` 原以固定由上而下 `if (...) return;` 漏斗運作，正常開局條件下前 8 項皆不成立，僅在少數固定單篇模板之間循環，且全數固定於每月 `Day == 15` 觸發，體感單調重複。
- **實作修改**：
  - `src\EventSystem.cs`：
    - 將韓戰爆發移回 **`1950/06/25`**（`korean_war_1950`：韓戰爆發與第七艦隊進入台海），並補齊 **`1950/10/25` `pva_korea_1950`**（中共抗美援朝北調・李彌滇緬孤軍出擊）、**`1951/05/01` `maag_1951`**（美軍顧問團駐台整編磋商）與 **`1952/04/28` `sino_japan_peace_1952`**（《中日和約》簽署暨外交地位角力）。
    - 重構 `CheckDynamic` 為 **7 大類加權候選池**（共用 `PickRotatedTemplate` 與類別／模板雙重冷卻），納入海空與關閉政策攔輪 (`BuildPatrolVarietyEvent` 3 變體)、經建/旅外學人歸國科研/美援物資配額 (`BuildEconVarietyEvent` 3 變體)、僑務/聯大代表權/戰地記者採訪 (`BuildDiploVarietyEvent` 3 變體)、破獲潛伏密電組/港澳難民潮/短波心戰/內閣路線爭 (`BuildIntelSocietyVarietyEvent` 4 變體)、對岸糧荒抗徵與技勤密線 (`BuildPrcCrackVarietyEvent` 2 變體)、五大戰區態勢 (`BuildTheaterFrontEvent`) 與 12 模板軍事部署 (`CreateMilitaryDeploymentEvent`)。
  - `src\SimEngine.cs`：`AdvanceDay` 動態檢定日改為每月浮動日 `dynDay = 5 + ((s.Year * 17 + s.Month * 11) % 22)`，不再每月死板卡在 15 日。
  - `src\Program.cs`、`DESIGN.md`：補齊測試 `5C`（驗證 `1950/06/25` 韓戰與 `1950/10/25` 抗美援朝準時觸發，及 36 個月動態輪替出現 `>=7` 種相異標題與 `>=4` 種相異標籤且無搶跑結束）；同步更新 `DESIGN.md` §九.3。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤（`EXE=2026-11-12T22:17:00` 與 DLL 一致）。
  - `國光：反攻大陸.exe --test`：退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B / 5C` 全部 `[PASS]`。
  - 已實際讀取 `ui_event_snapshot.png` 確認實機觸發非軍事新事件《情治單位破獲潛伏密電小組！》圖文排版正常，並已清除暫存圖與 `test_report.txt`。

### 2026-11-12 22:48 — 提升國庫券公債額度、新增財政／經貿自動發行與擴充科研至 63 項
- **問題定位與設計**：
  1. 原 `發行愛國公債` (`bonds`) 僅籌資 `GDP × 5%`（1950 年僅 `$0.70 億`），遠低於單支艦隊/聯隊造價（`$0.75 億`）與擴軍整編需求，且 `ActionsUsed` 全年鎖定一次；外交與經貿行動（`jp_trade`、`lobby_us`、`infra` 等）亦欠缺自動續行機制。
  2. 原科技樹共 `40` 項，其中 `情蒐(5)`、`策反(5)`、`後勤(6)`、`科技(8)` 項目顯著偏少、`T4` 區段近乎空白。
- **實作修改**：
  - `src\SimEngine.cs`：
    - 新增 `BondAmount(s) = max(2.5 × PriceIndex, GDP × 0.18)`（1950 年 `$2.52 億`，為舊額度 ~3.6 倍）並同步調整 `bonds` 與 `repay` 上限。
    - 新增 `GameState.AutoActions`（預設開啟 `"bonds"` 與 `"jp_trade"`）、`ToggleAutoAction`、`SetAutoFiscalPreset` 與 `StepAutoActions`（支援 `bonds`、`repay`、`jp_trade`、`lobby_us`、`farm_team`、`infra`、`propaganda`、`stockpile`、`psy_drop` 9 項常規行動），於 `MonthlyTick` 與 `YearlyTick` 自動檢查水位與安全負債上限後執行並加上 `[自動]` 戰報前綴。
    - 為新增科研項目接入實質被動加成（`IntelLevel` / `FogError` / `HasIntel` 納入 `p2v_patrol`、`se_asia_listen`、`formosat_recon`；`Upkeep` 納入 `high_speed_logi` -8%；`ResolveSupply` 納入 `high_speed_logi` +25%；`PrcUpdate` 納入 `kinmen_horn`、`mainland_relief`、`cultural_psy`、`cyber_psy`）。
  - `src\GameData.cs` 與 `src\EventSystem.cs`：科技樹擴充至 **63 項**（`科技 13 · 軍備 20 · 情蒐 10 · 策反 10 · 後勤 10`），補齊 1950–2012 全期科技鏈並同步更新起義/投誠被動加成（`defector_award`、`cyber_psy`）。
  - `src\MainForm.cs`：補齊所有 23 項新科技之向量圖示對應（`GetTechIconKind`）；於「外交、內政與大陸心戰」抽屜加入頂部一鍵開關列與各支援行動卡片獨立 `[自動✓ / 自動]` 切換鈕及最新公債單期金額顯示（已實機截圖並修正標籤換行與按鈕距）。
  - `src\Program.cs`：更新科技樹項目數斷言為 `63`（每分支至少 `10` 項），調整 `Test 4B` 簡易模式旗標檢查避免因隨機動態事件含「情報/秘密」字眼導致偶發假失敗，並新增測試 **`5D`** 驗證公債提升至 `$2.52 億`、跨月自動發債與對日經貿、開啟自動償債、新科研被動效果與 UI 抽屜／科技樹渲染。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤（`EXE` 與 `DLL` 時間戳 `2026/11/12 22:47:51` 一致）。
  - `國光：反攻大陸.exe --test`：退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B / 5C / 5D` 全部 `[PASS]`。
  - 已實際檢視 `ui_dip_auto_snapshot.png`（外交抽屜自動開關列無換行擠壓與 `$2.52 億` 顯示）、`ui_tech_intel_snapshot.png`（10 項情蒐樹）、`ui_tech_science_snapshot.png`（13 項科技樹）與 `ui_tech_snapshot.png`（20 項軍備樹），確認排版無重疊後已清理暫存截圖與 `test_report.txt`。

### 2026-11-13 10:50 — 徹底移除外交政策 Page 與底部導航抽屜按鈕
- **需求與改動（G2 最小變更）**：
  - 因財政（國庫券/公債）與經貿已全面轉為後台自動執行，依使用者指示徹底移除手動「外交政策 / 外交心戰」頁面。
  - `src\MainForm.cs`：
    - 自底部導航列 `_dockBar` 移除 `("dip", "✉ 外交心戰")` 按鈕，保留精簡五大導航按鈕（`⚔ 建軍後勤`、`⚙ 科技樹`、`◎ 軍情處`、`▤ 兩岸國力`、`★ 戰報日誌`）。
    - 移除右側抽屜 `_activeDrawer == "dip"` 分支與 `RenderDipDrawer` 函式（-123 行），主介面不再存在任何手動外交政策抽屜元件。
  - `src\Program.cs`：更新測試 `5D` 斷言確認主介面無「外交」按鈕且無任何外交抽屜元件殘留，自動發債與經貿持續於後台正確運作。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤（`EXE` 與 `DLL` 時間戳 `2026/11/13 10:50:43` 一致）。
  - `國光：反攻大陸.exe --test`：退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B / 5C / 5D` 全部 `[PASS]`。
  - 已實際讀取 `ui_dock_snapshot.png` 確認底部導航列排版整齊無外交按鈕，隨後已清除暫存圖與 `test_report.txt`。

### 2026-11-13 10:56 — 國防公債純粹錨定 GDP 18% 並仿傚美軍二戰 War Bonds 模式
- **需求與改動**：
  - 依使用者要求「國庫券應與gdp%有關、可仿傚美國的戰爭卷」，徹底移除 `BondAmount` 與 `AutoBondThreshold` 中原本夾帶的物價指數常數保底，改為純粹且嚴格與台灣 GDP% 關聯：
    - `WarBondGdpPct = 0.18`（募集規模嚴格等於 GDP 18%，1950 年為 $2.52 億，隨經濟成長等比擴增）。
    - `WarBondLowWaterPct = 0.04`（國庫低於 GDP 4% 即自動續發 War Bonds）。
    - 仿傚美軍二戰 War Bonds 模式：發行國防戰爭公債時提振戰時士氣（民心 +1），日誌與說明皆明確標示「仿美發行國防戰爭公債（War Bonds）」。
    - 頂部 HUD 國庫膠囊懸停提示（`UpdateHudTooltip`）即時展示當前戰爭公債單期額度（$X.XX 億 · 佔 GDP 18%）與年息 4% 累計公債。
- **驗證與檢查（Rule G5）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤（`EXE` 與 `DLL` 時間戳 `2026/11/13 10:56:57` 一致）。
  - `國光：反攻大陸.exe --test`：退出碼 `0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B / 5C / 5D` 全部 `[PASS]`。
  - 清除暫存截圖與 `test_report.txt`，同步 F 槽歸檔。





## 2026-11-13 — 主選單時代紀錄點資訊精簡

- **G1 證據**：修改前的主選單畫面中，未解鎖按鈕以固定 556×40 px 顯示標題、狀態與完整解鎖條件；紀錄點 III 一列被按鈕寬度截斷。`src/MainForm.cs` 的 `RefreshTitleOverlay` 原先直接將 `spec.Title` 與完整 `RequirementDesc` 合併至單行按鈕文字。
- **修正（`src/MainForm.cs`）**：未解鎖按鈕顯示「紀錄點序號＋條件摘要」，完整條件保留於 ToolTip 與 `AccessibleDescription`；已解鎖按鈕顯示序號、年份、光復省數與 GDP，並保留完整紀錄點名稱於輔助描述。
- **驗收條件**：主選單三個時代紀錄點在 1600×930 截圖中可辨識且摘要不被截斷；輔助描述包含完整條件；鎖定狀態與開局入口行為不變。
- **驗證（G3/G5）**：`dotnet build -c Release --no-restore` 成功，0 警告、0 錯誤；`國光：反攻大陸.exe --test` 結束碼 0，既有測試 1、2、3、4、4B、4C、5、5B、5C、5D 均通過；檢視 `bin/Release/net8.0-windows/ui_title_snapshot.png` 確認紀錄點列完整可讀。
- **修正過程**：第一輪精簡字串移除了可供既有 UI 測試定位的「紀錄點 I/II/III」前綴，測試在 `FindButtonWithText` 斷言失敗；恢復序號前綴後重跑，所有既有測試通過。
- **限制**：自動化測試的截圖輸出會覆寫同名檔，因此修改前的基準圖未另存；修改前的截斷情形已在本次工作中實際檢視。

## 2026-11-13 — UI 可讀性、結算兩岸國力與多尺寸驗收

- **主戰局低寬度資訊密度（`src/MainForm.cs`）**：基準 1280×760 戰局圖中，敵軍腹地兵力牌在東北與沿海密集重疊。當視窗地圖比例小於 `0.9` 且縮放不超過 `1.25×` 時，腹地敵軍兵力改由懸停查看；選取省、交戰／前線省、重點目標與可行動省仍顯示兵力。圖例同步提示腹地兵力可懸停查閱。
- **兩岸國力圖資料不足狀態（`src/MainForm.cs`）**：零年度資料顯示空狀態；只有一個年度資料點時顯示目前年份與趨勢提示，保留兩方資料點。
- **結算頁加入兩岸國力（`src/MainForm.cs`）**：結算畫面同時呈現 GDP 規模與綜合軍力歷史圖；結算摘要依文字尺寸調整，重新開局按鈕置中。以最小視窗截圖檢查圖表、結算摘要與操作入口無裁切。
- **共用按鈕鍵盤焦點（`src/WarUiControls.cs`）**：`WarButton` 在獲得／失去焦點時重繪，聚焦時加繪黃銅內框；自測確認返回戰局按鈕可取得焦點並檢視 `ui_keyboard_focus_snapshot.png`。
- **UI 自測擷取（`src/Program.cs`）**：增加建軍、兩岸國力、最小視窗主選單／戰局、結算與焦點畫面擷取；檢視 `ui_build_drawer_snapshot.png`、`ui_chart_drawer_snapshot.png`、`ui_title_min_snapshot.png`、`ui_game_min_snapshot.png`、`ui_ending_snapshot.png`。
- **最終驗證（G3/G5）**：`dotnet build -c Release --no-restore` 成功，0 警告、0 錯誤；最後一次 `國光：反攻大陸.exe --test` 結束碼 0，既有 `1 / 2 / 3 / 4 / 4B / 4C / 5 / 5B / 5C / 5D` 全部 `[PASS]`。
- **未完成的驗收**：戰報日誌右側抽屜可由按鈕開啟，但 `DrawToBitmap` 與 Windows `PrintWindow` 都在該狀態阻塞，故未取得該抽屜截圖；全版軍情處戰報列表已目視檢視，日誌抽屜渲染程式亦已檢查。跨模型交叉審查未執行，因預設原始輸出目錄在本次授權寫入範圍之外。


## 2026-11-13 — 建軍抽屜兵種科技連結補齊（依原始碼還原）

- **說明**：`RenderBuildDrawer` 在四大可造兵種卡加上對應關聯科技狀態列與「查看科技 →」按鈕，直接連動 `OpenTechPageAt` 開啟科技樹對應節點；`GameData.UnitTypes["arm"].ReqTech` 為 `"armor"`。
- **改動（`src/MainForm.cs`）**：依 `inf -> us_reorg`、`arm -> armor`、`fleet -> lst_fleet`、`air -> jets` 顯示「已完成／研發中／可研發／前置未完成」狀態與當前軍種科技／運力摘要，並保留 `CanBuild` 檢定與編成按鈕。
- **限制**：本段原文於前次紀錄編碼損毀，係依 `src/MainForm.cs:3576-3649` 與 `src/GameData.cs:337` 實況還原。
- **待補**：`GameData`、`SimEngine` 與科技樹對照關係維持不變。


## 2026-11-16 — 整體優化與自創內容計畫 v1（規劃，未改碼）

- 計畫全文：`OPTIMIZATION_PLAN.md`（同步至 `F:\DOC\misc\roc-grand-strategy-2d-game-20261110\`）。
- 盤點證據：build 0 警告 0 錯誤（16:46 實跑）；`AssessPosture`/`FrontHeadline` grep 0 次，S1–S5 未實作；本檔 432–437 行為字面 `?` 損毀；git 僅 1 commit；無批次模擬工具。
- 路線：P0 工程地基＋`--sim` 批次模擬 → P1 S1–S5 → P2 季節窗口/時代國策/光復區治理/參謀四派 → P3 作戰計畫與欺敵/敵後網路/中共戰略人格 → P4 變局劇本/回憶錄/新結局 → P5 平衡驗收。

## 2026-11-16 — 整體優化與十大戰略機制（P0–P5）實作驗收完成

- **使用者裁決與實施範圍**：分階段完成 P0 地基與 `--sim`、P1（`S1–S5` 態勢感知與比例縮放）、P2–P4（十大低微操高策略機制 `A–J`）與 P5 平衡調校與全自動驗證。參謀四派（`StaffFactions`）採全虛構幕僚長（雷振山、沈海平、顧青雲、林仲衡）；保留 `EventChoice.Stance` 並以獨立 `FactionTag` 支援派系歸屬；新增 `junta`、`fortress` 共 16 種結局；全新增UI維持嚴格 BMP 字元且不含「史實／架空」字樣。
- **P5 平衡調整與實測發現（`SimEngine.cs`）**：
  - 調整前初測（`--sim 5 --policy all --seed 1000`）發現 `rush1950` 於 `1952–1955` 即速推全國 26 省、`passive` 於 3/5 種子無作為卻因事件持續消耗 `PrcStab` 而墜入 `warlordP`。
  - 根因修正（`SimEngine.cs`）：`PrcAi` 於國軍深入 2 省以上啟動全國緊急總動員預備兵力並自縱深抽調 28% 餘力補防前線、3 省以上降低反撲門檻（`3.0 -> 2.15`，誘敵深入學說 `1.90`）；`PrcUpdate` 穩定度向均衡點回復係數由 `0.08` 調升為 `0.22`。
- **最終驗證結果（`--test` 與 `--sim`）**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤。
  - `國光：反攻大陸.exe --test`：`TEST_EXIT=0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B / 5C / 5D / 5E / 5F` 全數 `[PASS]`，且無殘留 HTML 與非 BMP 代理字元。
  - `國光：反攻大陸.exe --sim 5 --policy all --seed 1000 --out sim_final.csv`（20 局全數 `ZeroSixMonthStreaks = 0`）：
    - `passive`：3× `status`（0 省）、2× `beachhead`（1 省），無 `warlordP`、無 `full`。
    - `econ`：3× `armistice`（1980–1981，14–22 省）、1× `armistice`（2026，0 省）、1× `full`（1999，26 省）。
    - `rush1950`：5× `full` 拉長至 `1959–2001`（不再 2–4 年速推）。
    - `opportunist`：5× `split_ussr`（2026，17–25 省，北疆須應對蘇聯干涉）。
- **清理與歸檔**：已清除 `bin\Release\net8.0-windows\` 內測試暫存檔，完整工作區已同步歸檔至 `F:\DOC\misc\roc-grand-strategy-2d-game-20261110\`。

## 2026-11-17 — 介面減法：移除戰報日誌、兩岸國力改為僅結算時呈現

- **需求落實**：
  1. **移除戰報日誌**：
     - 從底部導航列（`_dockBar`）移除「★ 戰報日誌」按鈕。
     - 移除右側「★ 統帥部戰報與參謀派系」抽屜渲染與對應的 `State.Logs` 歷史電文捲軸視圖（`RenderIntelDrawer`）。
     - 清理存檔與紀錄點觸發之抽屜連帶刷新邏輯。
  2. **兩岸國力改為僅結算時呈現**：
     - 從底部導航列（`_dockBar`）移除常駐「▤ 兩岸國力」按鈕及右側抽屜畫布 `_drawerChartBody`。
     - 兩岸 GDP 規模演變與綜合軍力指數對比折線圖（`DrawLineChart`）保留並專用於對局終局結算彈窗（`ShowGameOverOverlay` / `historyCharts`）。
     - 更新 `_lblDockTech` 科技跑馬燈動態排版起點，自適應 3 顆精簡核心按鈕（建軍後勤、科技樹、軍情處）。
  3. **測試與設計同步**：
     - `src/Program.cs` 新增斷言驗證主介面不含「戰報日誌」與常駐「兩岸國力」入口，並確認結算畫面正常呈現國力曲線。
     - 執行 `dotnet test` / `國光：反攻大陸.exe --test`：全數測試通過（100% PASS）。


## 2026-11-16 — 全數 16 種結局 1950–2025 模擬實跑驗證（test 5G）

- **問題與排查（G1 證據）**：以初始 `--sim 25 --policy all` 實跑時僅能觸發 5 種結局（`full`、`armistice`、`split_ussr`、`status`、`beachhead`），其餘 11 種結局在純模擬推進中出現以下系統性阻斷：
  1. `warlordR`：`SimEngine.CheckEnd` 每年元旦在未觸發累加條件時無條件將 `s.Flags["warlordRisk"] = 0` 清零，導致門檻 `>= 3` 難累積。
  2. `warlordP`／`collapse`：`SimEngine.PrcUpdate` 的穩定度均衡拉力 `(target - s.PrcStab) * 0.20` 在低穩定度時產生強正向拉回，使極限地下顛覆（`underground`／`united_subvert`／`warlordPact`）仍難以把 `PrcStab` 壓在 `<= 0`～`10` 並維持至 2025 結算。
  3. 派系搶走和平路線：`InferChoiceFaction` 未充分涵蓋海空／情治／經貿選項且無高影響力遞減機制，和平取向在多事件中反覆加到 `army` 超過 `80`，誤轉為 `junta`。
  4. `end_invaded` 台灣失守後事件反撲：`1991 蘇聯解體`等事件透過 `SimEngine.AddUnit(s, "arm", "TW", 95)` 直接把國軍裝甲師空投進共軍已佔領的 `"TW"` 並導致台北再度易手。
- **修正落點（`SimEngine.cs`、`BatchSim.cs`、`Program.cs`）**：
  - `SimEngine.CheckEnd`：`warlordRisk` 僅於 `held.Count == 0 && s.Morale >= 65 && s.SupplyRatio >= 0.95` 時歸零，其餘維持既得割據累計。
  - `SimEngine.PrcUpdate`：計入 `radio_liberty`／`cyber_psy`／`cultural_psy`／`underground`／`united_subvert`、平均動盪（`> 22`）、地下網總級數與 `warlordPact` 持續穩定度扣減，當 `s.PrcStab <= 12.0 && (subvDrain >= 5.0 || s.HasFlag("warlordPact"))` 時取消回穩拉力。
  - `SimEngine.InferChoiceFaction`／`ApplyChoiceFactionShift`：補齊海空、情治、經貿關鍵字判定並於影響力 `>= 64`／`>= 76` 套用遞減係數。
  - `SimEngine.AddUnit`／`NearestRocNode`：事件欲於 `"TW"` 編組陸軍部隊時，若本島已非國軍控制則改派至 `NearestRocNode(s, "TW")`（涵蓋仍受國軍控制之離島備援）。
  - `BatchSim.cs` & `Program.cs`：新增 16 種結局對應策略（`EndingArchetypePolicies`：`end_full`～`end_bankrupt`）、`--policy endings` CLI 參數、`BatchSim.RunEndingCoverageSuite(1000u)` 與 `--test` 第 `5G` 項自動化覆蓋驗證（全程經由 `SimEngine.AdvanceDay` + `EventSystem.ResolveChoice` + 合法行動決策執行，無直接注入結局狀態）。
- **最終驗證結果**：
  - `dotnet build -c Release --no-restore`：0 警告、0 錯誤。
  - `國光：反攻大陸.exe --sim 1 --policy endings --seed 1000`（`EXIT=0`，16/16 精準一對一達成）：
    - `end_full -> full`（`1961`, `26` 省）、`end_collapse -> collapse`（`2026`, `10` 省）、`end_split_ussr -> split_ussr`（`2026`, `19` 省）、`end_armistice -> armistice`（`1981`, `22` 省）、`end_nanbei -> nanbei`（`2026`, `8` 省）、`end_beachhead -> beachhead`（`2026`, `1` 省）、`end_warlordP -> warlordP`（`2026`, `0` 省）、`end_warlordR -> warlordR`（`2026`, `3` 省）、`end_junta -> junta`（`2026`, `0` 省）、`end_mutiny -> mutiny`（`2026`, `0` 省）、`end_fortress -> fortress`（`2026`, `0` 省）、`end_status -> status`（`2026`, `0` 省）、`end_pianan -> pianan`（`2026`, `0` 省）、`end_invaded -> invaded`（`2026`, `0` 省）、`end_annexed -> annexed`（`2026`, `0` 省）、`end_bankrupt -> bankrupt`（`2026`, `0` 省）；全數 `ZeroSixMonthStreaks = 0`。
  - `國光：反攻大陸.exe --test`：`TEST_EXIT=0`，`1 / 2 / 3 / 4 / 4B / 5 / 5B / 5C / 5D / 5E / 5F / 5G` 全數 `[PASS]`。
  - `國光：反攻大陸.exe --sim 5 --policy all --seed 1000`（`SIM_ALL_EXIT=0`，20 局全數 `ZeroSixMonthStreaks = 0`）：`passive`（1× `status`、4× `junta`）、`econ`（5× `armistice` 1980–1981 含 15–22 省）、`rush1950`（5× `full` 1958–1993 含 26 省）、`opportunist`（3× `full` 2006–2026 含 20–26 省、2× `warlordR` 2026 含 21 省）。
