/*
OPTIMIZACIÓN DE RED - PRESUPUESTO DE ANCHO DE BANDA
====================================================

OBJETIVO: Minimizar uso de red manteniendo experiencia de juego fluida

PRINCIPIOS DE OPTIMIZACIÓN
===========================

1. USAR NETWORKED PROPERTIES SOLAMENTE PARA ESTADO CRÍTICO

   ✓ CRITICAL (Debe ser [Networked]):
     - Posición del jugador
     - Salud
     - Armas actuales
     - Puntuación y rachas
     - Estado de partida global

   ✗ NO CRÍTICO (Usar events/RPCs):
     - Animaciones de disparo
     - Efectos de impacto
     - Sonidos
     - Mensajes de kill feed

2. USAR TICK RATE APROPIADO

   Recomendado para multiplayer 2-4 jugadores:
   - UpdateInterval: 50ms (20 ticks/segundo)
   - Suficiente para movimiento suave
   - No genera overhead innecesario

3. COMPRESIÓN DE DATOS

   PlayerMovement:
   - Vector3 posición: 3 floats (12 bytes)
   - Quaternion rotación cámara: 4 floats (16 bytes)
   - Dirección movimiento: 2 shorts (4 bytes)
   
   Total: ~32 bytes por jugador por tick

4. CULLING DE PROPIEDADES

   Solo actualizar propiedades que realmente cambian:

   ✓ CORRECTO:
   [Networked] public int Health { get; set; } // Solo si cambia
   [Networked] public NetworkString<_32> PlayerName { get; set; }

   ✗ INCORRECTO:
   [Networked] public Vector3 LastFrameVelocity { get; set; } // Innecesario

5. RPC OPTIMIZATION

   Usar RPCs para eventos raros (kills, explosiones):
   
   [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
   void RPC_KillEvent(string killer, string victim) // ~64 bytes

   Vs.

   [Networked] public NetworkString KillerName { get; set; } // Persistente

ESTIMACIÓN DE ANCHO DE BANDA (2-4 Jugadores)
==============================================

POR TICK (50ms):
  - PlayerMovement x 4:    4 × 32 bytes = 128 bytes
  - Health updates (rare): ~20 bytes
  - GameState updates:     ~40 bytes
  SUBTOTAL: ~188 bytes/tick

POR SEGUNDO:
  - Sync: 188 × 20 ticks = 3,760 bytes/s ≈ 30 kbps
  - RPCs (disparos):      ~2 kbps
  - Eventos (kills):      ~1 kbps
  TOTAL: ~33 kbps ≈ 4 KB/s

PARA 2 HORAS DE JUEGO:
  4 KB/s × 3600 × 2 = 28.8 MB (Aceptable)

LISTA DE VERIFICACIÓN DE OPTIMIZACIÓN
======================================

- [ ] Networking enabled en Inspector
- [ ] UpdateTickRate: 20 Hz
- [ ] OnlyDirtyProperties enabled (si disponible)
- [ ] No snapshots innecesarios
- [ ] RPC targets específicos (no RpcTargets.All si no es necesario)
- [ ] OnChanged callbacks no hacen operaciones pesadas
- [ ] No sincronizar arrays/listas grandes
- [ ] Autoridad de servidor para validación
- [ ] Culling de distancia si es posible
- [ ] LOD para objetos lejanos

PROBLEMAS COMUNES Y SOLUCIONES
===============================

PROBLEMA: Alta latencia en daño
SOLUCIÓN: 
  - Usar prediction client-side
  - Validar en servidor
  - Replicar resultado

PROBLEMA: Desincronización de posiciones
SOLUCIÓN:
  - Aumentar frecuencia de updates
  - Usar interpolation en cliente
  - Network transform con compression

PROBLEMA: Explosión de tráfico en firefights
SOLUCIÓN:
  - Batch RPCs de disparos
  - Limitar numero de bullets simultáneos
  - Server-side bullet simulation

MÉTRICAS MONITOREABLES
======================

En el juego, mostrar:
  - Ping actual (RTT)
  - Paquetes enviados/recibidos por segundo
  - % packet loss
  - Bytes/segundo
  - Ticks perdidos

Ejemplo HUD:
  "Ping: 45ms | 120 pps | 0% loss | 35 KB/s"

TESTING EN CONDICIONES REALES
=============================

1. LAN (esperado perfecto):
   - Ping: <5ms
   - Loss: 0%
   - Ancho: 50+ KB/s disponible

2. WAN Local (espacio casa):
   - Ping: 20-50ms
   - Loss: <1%
   - Ancho: 10 MB/s disponible (más que suficiente)

3. WAN Internacional:
   - Ping: 100-300ms
   - Loss: <2%
   - Ancho: 2 MB/s (aún más que suficiente)

CONCLUSIÓN
==========

El presupuesto actual (~33 kbps) es EXTREMADAMENTE EFICIENTE.
Permite:
  - Jugar en cualquier conexión >100 kbps
  - Múltiples instancias simultáneas
  - Uso muy bajo de datos

NO necesita optimización adicional compleja.
El sistema está bien diseñado para WAN real.

*/
