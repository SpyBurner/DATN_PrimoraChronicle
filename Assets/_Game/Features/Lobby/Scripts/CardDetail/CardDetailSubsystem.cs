using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class CardDetailSubsystem : ICardDetailSubsystem
{
    [Inject] private readonly ICardDetailController _controller;
    [Inject] private readonly ICardDetailModel _model;

    public event UnityAction<string> CardNameChanged;
    public event UnityAction<string> CardDescriptionChanged;
    public event UnityAction<int> CardCostChanged;
    public event UnityAction<int> CardPowerChanged;
    public event UnityAction<string> CardImageUrlChanged;
    public event UnityAction<string> SkillNameChanged;
    public event UnityAction<string> SkillDescriptionChanged;
    public event UnityAction<string> SkillPatternChanged;

    public void Initialize()
    {
        if (_model?.CardName != null)
            _model.CardName.OnChanged += HandleCardNameChanged;

        if (_model?.CardDescription != null)
            _model.CardDescription.OnChanged += HandleCardDescriptionChanged;

        if (_model?.CardCost != null)
            _model.CardCost.OnChanged += HandleCardCostChanged;

        if (_model?.CardPower != null)
            _model.CardPower.OnChanged += HandleCardPowerChanged;

        if (_model?.CardImageUrl != null)
            _model.CardImageUrl.OnChanged += HandleCardImageUrlChanged;

        if (_model?.SkillName != null)
            _model.SkillName.OnChanged += HandleSkillNameChanged;

        if (_model?.SkillDescription != null)
            _model.SkillDescription.OnChanged += HandleSkillDescriptionChanged;

        if (_model?.SkillPattern != null)
            _model.SkillPattern.OnChanged += HandleSkillPatternChanged;
    }

    public void Dispose()
    {
        if (_model?.CardName != null)
            _model.CardName.OnChanged -= HandleCardNameChanged;

        if (_model?.CardDescription != null)
            _model.CardDescription.OnChanged -= HandleCardDescriptionChanged;

        if (_model?.CardCost != null)
            _model.CardCost.OnChanged -= HandleCardCostChanged;

        if (_model?.CardPower != null)
            _model.CardPower.OnChanged -= HandleCardPowerChanged;

        if (_model?.CardImageUrl != null)
            _model.CardImageUrl.OnChanged -= HandleCardImageUrlChanged;

        if (_model?.SkillName != null)
            _model.SkillName.OnChanged -= HandleSkillNameChanged;

        if (_model?.SkillDescription != null)
            _model.SkillDescription.OnChanged -= HandleSkillDescriptionChanged;

        if (_model?.SkillPattern != null)
            _model.SkillPattern.OnChanged -= HandleSkillPatternChanged;
    }

    public Task LoadCard(string cardId) => _controller.LoadCard(cardId);

    private void HandleCardNameChanged()
    {
        try { CardNameChanged?.Invoke(_model.CardName.Value); } catch { }
    }

    private void HandleCardDescriptionChanged()
    {
        try { CardDescriptionChanged?.Invoke(_model.CardDescription.Value); } catch { }
    }

    private void HandleCardCostChanged()
    {
        try { CardCostChanged?.Invoke(_model.CardCost.Value); } catch { }
    }

    private void HandleCardPowerChanged()
    {
        try { CardPowerChanged?.Invoke(_model.CardPower.Value); } catch { }
    }

    private void HandleCardImageUrlChanged()
    {
        try { CardImageUrlChanged?.Invoke(_model.CardImageUrl.Value); } catch { }
    }

    private void HandleSkillNameChanged()
    {
        try { SkillNameChanged?.Invoke(_model.SkillName.Value); } catch { }
    }

    private void HandleSkillDescriptionChanged()
    {
        try { SkillDescriptionChanged?.Invoke(_model.SkillDescription.Value); } catch { }
    }

    private void HandleSkillPatternChanged()
    {
        try { SkillPatternChanged?.Invoke(_model.SkillPattern.Value); } catch { }
    }
}
