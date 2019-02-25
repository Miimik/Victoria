﻿using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Victoria.Entities;
using Victoria.Entities.Enums;
using Victoria.Entities.Payloads;
using Victoria.Entities.Responses;
using Victoria.Helpers;

namespace Victoria
{
    public abstract class BaseLavaClient
    {
        #region EVENTS
        /// <summary>
        /// Fires when stats are sent from Lavalink server.
        /// </summary>
        public event Func<ServerStats, Task> StatsReceived;

        /// <summary>
        /// Fires when a track has timed out.
        /// </summary>
        public event Func<LavaPlayer, LavaTrack, long, Task> TrackStuck;

        /// <summary>
        /// Fires when a track throws an exception.
        /// </summary>
        public event Func<LavaPlayer, LavaTrack, string, Task> TrackException;

        /// <summary>
        /// Fires when player update is sent from lavalink server.
        /// </summary>
        public event Func<LavaPlayer, LavaTrack, TimeSpan, Task> PlayerUpdated;

        /// <summary>
        /// Fires when any of the <see cref="TrackReason"/> 's are met.
        /// </summary>
        public event Func<LavaPlayer, LavaTrack, TrackEndReason, Task> TrackFinished;
        #endregion

        public ServerStats ServerStats { get; private set; }

        private readonly BaseSocketClient _baseSocketClient;
        protected readonly ConcurrentDictionary<ulong, LavaPlayer> _players;
        private readonly SocketHelper _socketHelper;
        private SocketVoiceState cachedStated;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseSocketClient"></param>
        protected BaseLavaClient(BaseSocketClient baseSocketClient, Configuration configuration)
        {
            configuration.UserId = baseSocketClient.CurrentUser.Id;
            _players = new ConcurrentDictionary<ulong, LavaPlayer>();
            baseSocketClient.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
            baseSocketClient.VoiceServerUpdated += OnVoiceServerUpdated;

            _socketHelper = new SocketHelper();
            _socketHelper.OnMessage += OnMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voiceChannel"></param>
        /// <param name="textChannel"></param>
        /// <returns></returns>
        public async Task<LavaPlayer> ConnectAsync(IVoiceChannel voiceChannel, ITextChannel textChannel = null)
        {
            if (_players.TryGetValue(voiceChannel.GuildId, out var player))
                return player;

            player = new LavaPlayer(voiceChannel, textChannel, _socketHelper);

            return player;
        }

        private bool OnMessage(string message)
        {
            var json = JObject.Parse(message);
            ulong guildId;

            if (json.TryGetValue("guildId", out var guildToken))
                guildId = ulong.Parse($"{guildToken}");

            var opCode = $"{json.GetValue("op")}";
            switch (opCode)
            {
                case "playerUpdate":

                    break;

                case "stats":
                    var stats = json.ToObject<ServerStats>();
                    ServerStats = stats;
                    StatsReceived?.Invoke(stats);
                    break;

                case "event":
                    var evt = json.GetValue("type").ToObject<EventType>();
                    switch (evt)
                    {
                        case EventType.TrackEnd:
                            break;

                        case EventType.TrackException:
                            break;

                        case EventType.TrackStuck:
                            break;

                        case EventType.WebSocketClosed:
                            break;

                        default:
                            break;
                    }

                    break;

                default:
                    break;
            }

            return true;
        }

        private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState currentState)
        {
            if (user.Id != _baseSocketClient.CurrentUser.Id)
                return;

            cachedStated = currentState;

            if (oldState.VoiceChannel != null && currentState.VoiceChannel is null)
            {
                if (!_players.TryGetValue(oldState.VoiceChannel.Id, out var player))
                    return;

                await player.DisposeAsync().ConfigureAwait(false);
                var destroy = new DestroyPayload(oldState.VoiceChannel.Guild.Id);
                await _socketHelper.SendPayloadAsync(destroy).ConfigureAwait(false);
            }
        }

        private Task OnVoiceServerUpdated(SocketVoiceServer server)
        {
            if (!server.Guild.HasValue || !_players.TryGetValue(server.Guild.Id, out var player))
                return Task.CompletedTask;

            var update = new VoiceServerPayload(server, cachedStated.VoiceSessionId);
            return _socketHelper.SendPayloadAsync(update);
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
        }
    }
}