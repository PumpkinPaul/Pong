// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using MoonTools.ECS;
using Nakama;
using System.Collections.Generic;

namespace Pong.NakamaMultiplayer;

/// <summary>
/// Responsible for mapping local / network players to the entities in the ECS
/// </summary>
public class PlayerEntityMapper
{
    const int INVALID_ENTITY = -1;

    readonly Dictionary<PlayerIndex, string> _playerIndexToSessionId = new();
    readonly Dictionary<PlayerIndex, Entity> _playerIndexToEntity = new();
    readonly Dictionary<string, Entity> _sessionIdToEntity = new();

    public void AddPlayer(PlayerIndex playerIndex, string sessionId)
    {
        _playerIndexToSessionId[playerIndex] = sessionId;
    }

    public void MapEntity(PlayerIndex playerIndex, Entity entity)
    {
        _playerIndexToEntity[playerIndex] = entity;

        var sessionId = _playerIndexToSessionId[playerIndex];
        _sessionIdToEntity[sessionId] = entity;
    }

    public void RemovePlayerBySessionId(string sessionId)
    {
        //TODO
    }

    public Entity GetEntityFromSessionId(string sessionId)
    {
        return _sessionIdToEntity[sessionId];
        //return INVALID_ENTITY;
    }
}
