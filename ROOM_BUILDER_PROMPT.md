# MASTER PROMPT: THIẾT KẾ PHÒNG LAB MÁY TÍNH THỰC TẾ & KỊCH BẢN DIỄN TIẾN TỪ TỪ (PROGRESSIVE SCENARIO)

> **Mục đích:** Prompt chuẩn và Hướng dẫn Dựng Cảnh chi tiết dành cho AI Agent / Unity MCP để xây dựng một **Phòng Lab máy tính thực tế, sinh động (nhiều bàn ghế, máy tính, cửa, cửa sổ)** và cài đặt **Cơ chế Sự cố Bùng phát Từ từ (Progressive Emergency Event)** theo đúng kịch bản `game_scenario_script.md`.

---

## 🎯 PROMPT MẪU HOÀN CHỈNH DÀNH CHO AI AGENT / UNITY MCP

```text
Hãy sử dụng Unity MCP (hoặc C# Editor Script) để xây dựng hoàn chỉnh một Phòng Lab Máy tính Trường Đại học sinh động, chân thực và cài đặt Kịch bản Diễn tiến Sự cố Từ từ (Progressive Incident Flow) trong Unity dựa trên các gói Asset "POLYGON Office" và "Technical Laboratory Designer":

--------------------------------------------------------------------------------
1. KIẾN TRÚC PHÒNG KHÉP KÍN THỰC TẾ (10m x 8m x 3.5m):
--------------------------------------------------------------------------------
- Tỷ lệ không gian: Trục X (Rộng 10m từ -5m đến +5m), Trục Z (Dài 8m từ -4m đến +4m), Trục Y (Cao 3.5m).
- Sàn nhà (Floor): Lát gạch caro/bề mặt bê tông mượt `SM_Bld_Floor_Tiles_01.prefab` hoặc `SM_Bld_Floor_Panel_01.prefab` tại Y = 0.
- Vách Tường Nam (South Wall, Z = -4m): 
  * Gắn Cửa chính thoát hiểm gỗ/gỗ kính `SM_Bld_Door_01.prefab` tại trung tâm (X = 0, Y = 0).
  * Gắn Nút bấm báo cháy khẩn cấp đỏ `SM_Prop_FireAlarm_Switch_01.prefab` bên phải cửa (X = +0.8m, Y = 1.4m).
  * Treo giá đỡ Bình chữa cháy CO2 `SM_Prop_Fire_Extinguisher_01.prefab` bên trái cửa (X = -1.2m, Y = 1.2m).
- Vách Tường Bắc (North Wall, Z = +4m):
  * Gắn Bảng trắng giảng dạy lớn `SM_Prop_Whiteboard_01.prefab` ở giữa tường.
  * Góc Đông-Bắc đặt Tủ Server chính `Server_full.prefab` (X = +3.5m, Z = +3.0m).
- Vách Tường Đông (East Wall, X = +5m):
  * Gắn Tủ điện Cầu dao Aptomat `panel_for_server.prefab` (X = +4.8m, Y = 1.5m, Z = +1.0m).
- Vách Tường Tây (West Wall, X = -5m):
  * Lắp 2 khung Cửa sổ kính lớn `SM_Bld_Window_01.prefab` lấy ánh sáng tự nhiên.
  * Treo Bình chữa cháy Bọt/Nước decoy `SM_Prop_Fire_Extinguisher_01.prefab` (X = -4.8m, Y = 1.2m, Z = 0.0m).
- Trần Nhà (Ceiling): Hệ thống trần thạch cao `SM_Bld_Ceiling_Panel_01.prefab` tại Y = 3.5m với 4 cụm đèn tuýp/đèn LED âm trần chiếu ánh sáng trắng sáng rõ.

--------------------------------------------------------------------------------
2. BỐ TRÍ DÃY MÁY TÍNH HỌC SINH (STUDENT WORKSTATIONS):
--------------------------------------------------------------------------------
Xếp 2 Dãy bàn máy tính thực hành song song chạy dọc phòng:
- Dãy 1 (Bên trái X = -2.0m, Z từ -1.5m đến +1.5m):
  * Đặt 3 bàn máy tính gỗ/kim loại `SM_Prop_Desk_01.prefab`.
  * Trên mỗi bàn trang bị: Màn hình PC `SM_Prop_Monitor_01.prefab`, Bàn phím, Chuột, Ổ cắm điện và Ghế xoay văn phòng `SM_Prop_Chair_01.prefab`.
- Dãy 2 (Bên phải X = +2.0m, Z từ -1.5m đến +1.5m):
  * Đặt 3 bàn ghế và bộ máy tính PC tương tự.
- Bàn Giảng viên (X = -3.5m, Z = +3.0m):
  * Đặt 1 bàn giáo viên có Laptop, micro và tài liệu giảng dạy.

--------------------------------------------------------------------------------
3. CƠ CHẾ DIỄN TIẾN TÌNH HUỐNG TỪ TỪ (PROGRESSIVE EVENT LOGIC):
--------------------------------------------------------------------------------
- PHASE 0 (Thực hành Bình thường - 0 đến 30 giây đầu):
  * Tất cả đèn trần sáng bình thường (White Lighting).
  * Hạt Lửa (`Fire.prefab`) và Khói (`Smoke.prefab`) ở trạng thái TẮT (`SetActive(false)`).
  * Sinh viên tự do di chuyển làm quen không gian và thao tác với máy tính.
- PHASE 1 (Bùng phát Sự cố Khẩn cấp - Khi kích hoạt Event):
  * Phát âm thanh nổ tụ điện / xẹt điện (`Electrical Spark SFX`).
  * Hạt Lửa trên Tủ Server bật sáng (`SetActive(true)`).
  * Đèn trần bắt đầu nhấp nháy đỏ khẩn cấp (Emergency Red Lights flickering).
  * Khói độc từ trần nhà tích tụ và hạ thấp dần theo thời gian.
```

---

## 🛠️ SCRIPT KHỞI TẠO NỘI THẤT DÃY MÁY TÍNH VÀ CỬA TRONG UNITY (C# CODE)

Bạn có thể chạy đoạn C# script dưới đây để tự động sinh toàn bộ Dãy bàn máy tính, Cửa ra vào và Cửa sổ vào Scene Unity:

```csharp
// C# Editor Script: PopulateLabInteriors.cs
using UnityEngine;
using UnityEditor;

public class PopulateLabInteriors
{
    [MenuItem("VR PCCC/Populate Student Workstations & Doors")]
    public static void Populate()
    {
        GameObject interiorParent = new GameObject("[LAB_INTERIORS]");

        // 1. Tạo 2 Dãy Bàn ghế Máy tính
        for (int i = 0; i < 3; i++)
        {
            float zPos = -1.5m + (i * 1.5f);

            // Dãy 1 (Bên trái X = -2.0)
            CreateWorkstation(interiorParent.transform, new Vector3(-2.0f, 0, zPos), "Desk_Left_" + i);

            // Dãy 2 (Bên phải X = +2.0)
            CreateWorkstation(interiorParent.transform, new Vector3(2.0f, 0, zPos), "Desk_Right_" + i);
        }

        Debug.Log("[VR PCCC] Đã khởi tạo xong 6 Bộ Bàn ghế Máy tính trong phòng Lab!");
    }

    private static void CreateWorkstation(Transform parent, Vector3 pos, string name)
    {
        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.name = name;
        desk.transform.parent = parent;
        desk.transform.position = pos + new Vector3(0, 0.4f, 0);
        desk.transform.localScale = new Vector3(1.2f, 0.8f, 0.7f);

        // Màn hình PC trên bàn
        GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        monitor.name = "PC_Monitor";
        monitor.transform.parent = desk.transform;
        monitor.transform.localPosition = new Vector3(0, 0.6f, 0.1f);
        monitor.transform.localScale = new Vector3(0.5f, 0.4f, 0.1f);
    }
}
```
