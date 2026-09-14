using System;

public static class PySnipGameplayConverter
{
    public static MapGameplayMetadata Convert(
        PySnipMapScriptData source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(
                nameof(source)
            );
        }

        MapTeamMetadata blueTeam =
            CreateBlueTeam(
                source
            );

        MapTeamMetadata greenTeam =
            CreateGreenTeam(
                source
            );

        MapGameplayMetadata gameplay =
            new MapGameplayMetadata();

        /*
         * Por enquanto o metadata nativo
         * está estruturado principalmente
         * para CTF.
         *
         * Depois será expandido para outros
         * modos do PySnip e Jagex.
         */
        gameplay.defaultGameMode =
            "ctf";

        gameplay.supportedGameModes =
            new string[]
            {
                "ctf"
            };

        gameplay.teams =
            new MapTeamMetadata[]
            {
                blueTeam,
                greenTeam
            };

        return gameplay;
    }

    // ============================================================
    // BLUE TEAM
    // ============================================================

    private static MapTeamMetadata CreateBlueTeam(
        PySnipMapScriptData source)
    {
        MapTeamMetadata team =
            new MapTeamMetadata();

        team.id =
            "blue";

        team.displayName =
            "Blue";

        // --------------------------------------------------------
        // SPAWN
        // --------------------------------------------------------

        if (source.HasCustomBlueSpawns)
        {
            team.spawnPolicy =
                MapLocationPolicy.MapDefined;

            team.spawnPoints =
                source.BlueSpawns.ToArray();
        }
        else
        {
            team.spawnPolicy =
                MapLocationPolicy.ServerDefault;

            team.spawnPoints =
                Array.Empty<MapCoordinate>();
        }

        // --------------------------------------------------------
        // BASE
        // --------------------------------------------------------

        if (source.HasBlueBase)
        {
            team.basePolicy =
                MapLocationPolicy.MapDefined;

            team.hasBase =
                true;

            team.basePosition =
                source.BlueBase;
        }
        else
        {
            team.basePolicy =
                MapLocationPolicy.ServerDefault;

            team.hasBase =
                false;

            /*
             * MapCoordinate é struct.
             *
             * Portanto não pode receber null.
             * O valor abaixo é ignorado enquanto
             * hasBase == false.
             */
            team.basePosition =
                default(MapCoordinate);
        }

        // --------------------------------------------------------
        // FLAG
        // --------------------------------------------------------

        if (source.HasBlueFlag)
        {
            team.flagPolicy =
                MapLocationPolicy.MapDefined;

            team.hasFlag =
                true;

            team.flagPosition =
                source.BlueFlag;
        }
        else
        {
            team.flagPolicy =
                MapLocationPolicy.ServerDefault;

            team.hasFlag =
                false;

            team.flagPosition =
                default(MapCoordinate);
        }

        return team;
    }

    // ============================================================
    // GREEN TEAM
    // ============================================================

    private static MapTeamMetadata CreateGreenTeam(
        PySnipMapScriptData source)
    {
        MapTeamMetadata team =
            new MapTeamMetadata();

        team.id =
            "green";

        team.displayName =
            "Green";

        // --------------------------------------------------------
        // SPAWN
        // --------------------------------------------------------

        if (source.HasCustomGreenSpawns)
        {
            team.spawnPolicy =
                MapLocationPolicy.MapDefined;

            team.spawnPoints =
                source.GreenSpawns.ToArray();
        }
        else
        {
            team.spawnPolicy =
                MapLocationPolicy.ServerDefault;

            team.spawnPoints =
                Array.Empty<MapCoordinate>();
        }

        // --------------------------------------------------------
        // BASE
        // --------------------------------------------------------

        if (source.HasGreenBase)
        {
            team.basePolicy =
                MapLocationPolicy.MapDefined;

            team.hasBase =
                true;

            team.basePosition =
                source.GreenBase;
        }
        else
        {
            team.basePolicy =
                MapLocationPolicy.ServerDefault;

            team.hasBase =
                false;

            team.basePosition =
                default(MapCoordinate);
        }

        // --------------------------------------------------------
        // FLAG
        // --------------------------------------------------------

        if (source.HasGreenFlag)
        {
            team.flagPolicy =
                MapLocationPolicy.MapDefined;

            team.hasFlag =
                true;

            team.flagPosition =
                source.GreenFlag;
        }
        else
        {
            team.flagPolicy =
                MapLocationPolicy.ServerDefault;

            team.hasFlag =
                false;

            team.flagPosition =
                default(MapCoordinate);
        }

        return team;
    }
}