/*
ARQUITECTURA DE AUTORIDAD - PHOTON FUSION (SHARED MODE)
=========================================================

Este documento describe cómo se implementa la autoridad de reglas en ShooterRed.

PRINCIPIOS CORE:
================

1. MASTERCLIENT ES LA AUTORIDAD GLOBAL
   - Photon Fusion asigna automáticamente un MasterClient
   - El MasterClient valida TODAS las acciones críticas:
     * Daño y eliminaciones
     * Puntuaciones y rachas
     * Desbloques de habilidades
     * Spawns de objetos especiales

2. TRES TIPOS DE AUTORIDAD EN NETWORKED PROPERTIES:

   a) StateAuthority:
      - Solo el propietario del objeto puede modificar
      - En GameState: solo MasterClient puede modificar estado global
      - En PlayerHealth: solo el servidor puede procesar daño
      
   b) InputAuthority:
      - El jugador local controla su propio input
      - Los inputs se envían al servidor para validación
      
   c) NO Authority (Read-only):
      - Otros clientes reciben cambios replicados
      - No pueden modificar el estado

FLUJO DE VALIDACIÓN - EJEMPLO: DISPARO
=======================================

1. CLIENTE LOCAL (InputAuthority)
   └─ Detecta input del jugador (click en ratón)
   └─ Realiza raycast LOCAL para predección
   └─ Envía RPC: "HitRequest" al servidor

2. SERVIDOR / MASTERCLIENT (StateAuthority de GameState)
   └─ Recibe RPC_HitRequest
   └─ Valida: ¿Existe el objetivo? ¿Es válido?
   └─ Calcula daño REAL
   └─ Modifica PlayerHealth (StateAuthority)
   └─ Llama a GameState.AddKill() (MasterClient authority)
   └─ Replica a todos los clientes

3. TODOS LOS CLIENTES
   └─ Reciben cambios replicados
   └─ Actualizan visualización
   └─ NO pueden modificar el estado

COMPONENTES CLAVE DE AUTORIDAD
===============================

Clase             | Authority        | Responsabilidad
────────────────────────────────────────────────────────
GameState         | MasterClient     | Árbitro global, puntuaciones, rachas
PlayerHealth      | Server/Owner     | Daño, respawn, eliminaciones
PlayerMovement    | InputAuthority   | Movimiento local, replicado a otros
PlayerHabilities  | StateAuthority   | Desbloqueos, activación de habilidades
MatchManager      | MasterClient     | Control de ciclo de partida

CAMBIO DE MASTERCLIENT
======================

Si el MasterClient se desconecta:

1. Photon AUTOMÁTICAMENTE designa nuevo MasterClient
2. GameState sigue siendo autoridad (ahora en nuevo MC)
3. El sistema CONTINÚA sin quiebres (Fusion maneja la migración)
4. No requiere migración manual de estado compleja

CÓMO VERIFICAR AUTORIDAD
==========================

En cada script que modifica estado CRÍTICO:

✓ CORRECTO:
  if (!HasStateAuthority) return;
  // modificar GameState, eliminar jugadores, etc

✓ CORRECTO:
  [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
  void RPC_RequestAction() { ... }

✗ INCORRECTO:
  // Modificar estado global sin verificación
  gameState.AddKill(killer, victim);

✗ INCORRECTO:
  // Permitir que clientes modifiquen jugador ajeno
  otherPlayer.Health = 0;

MECANISMOS DE SEGURIDAD IMPLEMENTADOS
======================================

1. Validaciones en PropertyChanged callbacks
2. RPCs con restricciones de source/target
3. GameState como árbitro centralizado
4. PlayerRef para identificar únicamente a jugadores
5. Checks de HasStateAuthority antes de modificaciones

EVENTOS SIN AUTORIDAD (Solo replicación)
=========================================

Eventos que SÍ se pueden enviar directamente sin validación previa:

- Kill feed entries (son informativos, no cambian gameplay)
- Efectos visuales (explosiones, impactos)
- Animaciones
- Sonidos

Estos usan: RpcTargets.All sin StateAuthority check

TESTING DE AUTORIDAD
====================

Para verificar que el sistema es robusto:

1. Desconectar MasterClient durante partida
   → Sistema debe continuar sin quiebres
   
2. Perder conexión brevemente
   → State debe ser consistente al reconectar
   
3. Dos clientes disparan simultáneamente
   → MasterClient valida ambos correctamente
   
4. Modificar valores localmente en client
   → Los cambios NO se replican (validación funciona)

*/
