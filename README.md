# 2026-PickPlace-backend

**PickPlace** adalah layanan backend untuk sistem peminjaman ruangan yang dibangun menggunakan **ASP.NET Core**. Proyek ini menyediakan API untuk mengelola data ruangan, peminjaman (*booking*), serta riwayat transaksi dengan fitur keamanan dan manajemen data yang efisien.

## Fitur Utama

Fitur yang sedang dikembangkan dan tersedia saat ini:

* **Manajemen Ruangan (`Room`)**:
    * CRUD data ruangan.
    * **Soft Delete**: Data ruangan tidak dihapus permanen untuk menjaga integritas riwayat peminjaman.
* **Sistem Booking**:
    * Peminjaman ruangan dengan validasi ketersediaan jadwal.
    * **Booking History**: Endpoint untuk melihat riwayat peminjaman user.
* **Integrasi & Keamanan**:
    * Konfigurasi **CORS Policy** untuk integrasi dengan Frontend.
    * Validasi input dan *exception handling*.

## Teknologi yang Digunakan

* **Framework**: ASP.NET Core 8.0
* **Bahasa**: C#
* **Database**: SQL Server
* **ORM**: Entity Framework Core
* **Tools**: Thunder Client (Dokumentasi API)

## Cara Menjalankan Project

Ikuti langkah ini untuk menjalankan backend di lokal komputer:

### 1. Clone Repository
```bash
git clone [https://github.com/elysiasrahma/2026-PickPlace-backend.git](https://github.com/elysiasrahma/2026-PickPlace-backend.git)
cd 2026-PickPlace-backend
