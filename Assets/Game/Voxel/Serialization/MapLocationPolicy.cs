public enum MapLocationPolicy
{
    /*
     * A posição foi explicitamente definida
     * pelo próprio mapa.
     *
     * Exemplo:
     * Bikini Bottom.txt
     */
    MapDefined = 0,

    /*
     * O mapa não definiu posição.
     *
     * O servidor deverá escolher uma posição
     * válida seguindo a regra padrão do modo.
     *
     * Exemplo:
     * Hallway.txt
     */
    ServerDefault = 1,

    /*
     * Essa entidade/localização não deve
     * existir para esse mapa/modo.
     */
    Disabled = 2
}