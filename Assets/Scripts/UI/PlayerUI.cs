using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Text _text;
    private Player _master;
    private Camera _mainCamera;
    private bool _isVisible = true;
    private ClientAuthoritativePlayerStateSnapshot _authoritativeSnapshot;
    private ClientCombatPresentationSnapshot _combatSnapshot = ClientCombatPresentationSnapshot.Empty;

    public void Init(Player master)
    {
        _canvas = this.transform.GetComponent<Canvas>();
        _mainCamera = Camera.main;
        this._master = master;
        RefreshText();
    }

    public void SyncAuthoritativeState(ClientAuthoritativePlayerStateSnapshot snapshot, ClientCombatPresentationSnapshot combatSnapshot)
    {
        _authoritativeSnapshot = snapshot;
        _combatSnapshot = combatSnapshot ?? ClientCombatPresentationSnapshot.Empty;
        RefreshText();
    }

    private void FixedUpdate()
    {
        if (_isVisible)
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            if (_mainCamera != null)
            {
                _canvas.transform.LookAt(_mainCamera.transform);
            }
        }
    }

    private void OnBecameVisible()
    {
        _isVisible = true;
    }

    private void OnBecameInvisible()
    {
        _isVisible = false;
    }

    private void RefreshText()
    {
        if (_text == null || _master == null)
        {
            return;
        }

        if (_authoritativeSnapshot == null)
        {
            _text.text = $"{_master.PlayerId}\nHP:?\nCombat:{FormatCombatLine()}";
            return;
        }

        _text.text = $"{_master.PlayerId}\nHP:{_authoritativeSnapshot.Hp} Tick:{_authoritativeSnapshot.Tick}\nCombat:{FormatCombatLine()}";
    }

    private string FormatCombatLine()
    {
        if (_combatSnapshot == null || !_combatSnapshot.HasLastEvent)
        {
            return _combatSnapshot != null && _combatSnapshot.IsDead ? "Dead" : "None";
        }

        var deadSuffix = _combatSnapshot.IsDead ? " Dead" : string.Empty;
        return $"{_combatSnapshot.LastEventType} Dmg:{_combatSnapshot.LastDamage} Tick:{_combatSnapshot.LastEventTick}{deadSuffix}";
    }
}
