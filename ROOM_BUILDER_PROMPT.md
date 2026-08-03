# MASTER PROMPT & SCENE BUILDER SPECIFICATION: VR PCCC COMPUTER LAB

> **Mục đích:** Prompt chuẩn và Hướng dẫn Kiến trúc Không gian 3D chi tiết dùng để ra lệnh cho AI Agent (hoặc tự động dựng bằng C# Script / Unity MCP) nhằm đóng gói hoàn chỉnh phòng Lab máy tính khép kín 10m x 8m x 3.5m theo đúng kịch bản `game_scenario_script.md`.

---

## 🎯 PROMPT MẪU DÀNH CHO AI AGENT / UNITY BUILDER

```text
Hãy sử dụng Unity MCP (hoặc C# Editor Script) để xây dựng hoàn chỉnh một phòng Lab Máy tính / Server khép kín (Enclosed Computer Lab Room) trong Unity Scene dựa trên gói 3D Asset "POLYGON Office" và "Technical Laboratory Designer" với các yêu cầu không gian chi tiết sau:

1. KÍCH THƯỚC PHÒNG & KHUNG KIẾN TRÚC (ROOM SHELL):
   - Kích thước phòng: Rộng 10m (Trục X từ -5m đến +5m) x Dài 8m (Trục Z từ -4m đến +4m) x Cao 3.5m (Trục Y từ 0m đến 3.5m).
   - Sàn nhà (Floor): Ghép các tấm sàn `SM_Bld_Floor_Tiles_01.prefab` hoặc `SM_Bld_Floor_Concrete_01.prefab` thành mặt sàn khép kín 10m x 8m tại Y = 0.
   - 4 Vách tường (Enclosed Walls): 
     * Tường Bắc (North Wall, Z = +4m): Tường sơn xám/trắng `SM_Bld_Wall_01`.
     * Tường Nam (South Wall, Z = -4m): Tường có gắn Cửa thoát hiểm khẩn cấp `SM_Bld_Door_01` (ở vị trí X = 0) và Nút báo cháy `SM_Prop_FireAlarm_Switch_01` (X = +0.8m).
     * Tường Đông (East Wall, X = +5m): Tường gắn Tủ cầu dao điện Aptomat `panel_for_server.prefab` (X = +4.8m, Y = 1.5m, Z = +1.0m).
     * Tường Tây (West Wall, X = -5m): Tường có ô cửa sổ kính và giá treo bình chữa cháy bọt/nước decoy.
   - Trần nhà (Ceiling): Lắp hệ thống trần thạch cao `SM_Bld_Ceiling_Panel_01` ở độ cao Y = 3.5m, xen kẽ 4 đèn LED âm trần `SM_Bld_Ceiling_Panel_Light_01` phát ánh sáng trắng (Intensity = 1.2).

2. BỐ TRÍ NỘI THẤT HỌC TẬP (STUDENT WORKSTATIONS):
   - Xếp 2 dãy bàn máy tính song song ở trung tâm phòng:
     * Dãy 1 (Bên trái X = -1.8m, Z từ -1m đến +1.5m): Gồm 3 bộ bàn `SM_Prop_Desk_01`, ghế xoay `SM_Prop_Chair_01`, trên bàn đặt PC Monitor, bàn phím, chuột và ổ cắm điện.
     * Dãy 2 (Bên phải X = +1.8m, Z từ -1m đến +1.5m): Gồm 3 bộ bàn ghế tương tự.

3. KHU VỰC SỰ CỐ & THIẾT BỊ AN TOÀN (HAZARD & SAFETY ZONES):
   - Zone A (Góc Đông-Bắc X = +3.5m, Z = +3.0m): Đặt Tủ Server chính `Server_full.prefab` với hệ thống hạt lửa `Fire.prefab` gắn ở độ cao Y = 1.2m.
   - Zone C1 (Góc Nam cạnh Cửa X = -1.2m, Y = 1.2m, Z = -3.8m): Treo Bình chữa cháy CO2 `SM_Prop_Fire_Extinguisher_01` có gắn component `XRGrabInteractable` và `FireExtinguisher (Type = CO2)`.
   - Zone C2 (Vách Tây X = -4.8m, Y = 1.2m, Z = 0.0m): Treo Bình chữa cháy Nước/Bọt decoy.

4. ÁNH SÁNG & KHÓI ĐỘC ÂM TRẦN (ATMOSPHERE & LIGHTING):
   - Đặt 1 vùng khói độc volumetric `Interactive Smoke.prefab` ở trần nhà (Y = 3.2m).
   - Đặt 1 đèn spotlight màu đỏ khẩn cấp `Emergency Red Light` ở giữa trần nhà (ban đầu tắt, bật lên khi lửa bùng phát).
```

---

## 🛠️ CÁC BƯỚC THỰC HIỆN DỰNG PHÒNG TỰ ĐỘNG BẰNG UNITY MCP

Dưới đây là mã lệnh C# Automation Script để tạo dựng toàn bộ Sàn, 4 Vách tường và Trần nhà kín trong Unity chỉ bằng 1 thao tác:

```csharp
// C# Editor Script: GenerateEnclosedRoom.cs
using UnityEngine;
using UnityEditor;

public class GenerateEnclosedRoom
{
    [MenuItem("VR PCCC/Generate Full Enclosed Room")]
    public static void BuildRoom()
    {
        GameObject roomParent = new GameObject("[ROOM_ENCLOSURE_SHELL]");
        
        // 1. Dựng Mặt Sàn (Floor 10m x 8m)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_Concrete";
        floor.transform.parent = roomParent.transform;
        floor.transform.position = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(10f, 0.2f, 8f);
        
        // 2. Dựng 4 Vách Tường (Walls 3.5m High)
        // Tường Bắc (North)
        GameObject wallNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallNorth.name = "Wall_North";
        wallNorth.transform.parent = roomParent.transform;
        wallNorth.transform.position = new Vector3(0, 1.75f, 4.0f);
        wallNorth.transform.localScale = new Vector3(10f, 3.5f, 0.2f);

        // Tường Nam (South)
        GameObject wallSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallSouth.name = "Wall_South";
        wallSouth.transform.parent = roomParent.transform;
        wallSouth.transform.position = new Vector3(0, 1.75f, -4.0f);
        wallSouth.transform.localScale = new Vector3(10f, 3.5f, 0.2f);

        // Tường Đông (East)
        GameObject wallEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallEast.name = "Wall_East";
        wallEast.transform.parent = roomParent.transform;
        wallEast.transform.position = new Vector3(5.0f, 1.75f, 0);
        wallEast.transform.localScale = new Vector3(0.2f, 3.5f, 8f);

        // Tường Tây (West)
        GameObject wallWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallWest.name = "Wall_West";
        wallWest.transform.parent = roomParent.transform;
        wallWest.transform.position = new Vector3(-5.0f, 1.75f, 0);
        wallWest.transform.localScale = new Vector3(0.2f, 3.5f, 8f);

        // 3. Dựng Trần Nhà (Ceiling)
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling_Drop";
        ceiling.transform.parent = roomParent.transform;
        ceiling.transform.position = new Vector3(0, 3.6f, 0);
        ceiling.transform.localScale = new Vector3(10f, 0.2f, 8f);

        Undo.RegisterCreatedObjectUndo(roomParent, "Build Enclosed Room");
        Debug.Log("[VR PCCC] Phòng khép kín 10m x 8m x 3.5m đã được dựng thành công!");
    }
}
```
