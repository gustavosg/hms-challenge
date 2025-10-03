namespace Shared.Services.Cache;

public interface ICacheService
{
    /// <summary>
    /// Obt�m um valor do cache
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <returns>Valor ou null se n�o existir</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Define um valor no cache com expira��o padr�o (5 minutos)
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="value">Valor a ser armazenado</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Define um valor no cache com expira��o espec�fica
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="value">Valor a ser armazenado</param>
    /// <param name="expiration">Tempo de expira��o</param>
    void Set<T>(string key, T value, TimeSpan expiration);

    /// <summary>
    /// Define um valor no cache com expira��o em data espec�fica
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="value">Valor a ser armazenado</param>
    /// <param name="absoluteExpiration">Data de expira��o absoluta</param>
    void Set<T>(string key, T value, DateTimeOffset absoluteExpiration);

    /// <summary>
    /// Obt�m um valor do cache ou executa a fun��o para obter e armazenar
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="getItem">Fun��o para obter o valor se n�o existir no cache</param>
    /// <param name="expiration">Tempo de expira��o (opcional)</param>
    /// <returns>Valor do cache ou resultado da fun��o</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null);

    /// <summary>
    /// Remove um item do cache
    /// </summary>
    /// <param name="key">Chave do cache</param>
    void Remove(string key);

    /// <summary>
    /// Verifica se uma chave existe no cache
    /// </summary>
    /// <param name="key">Chave do cache</param>
    /// <returns>True se existir, false caso contr�rio</returns>
    bool Exists(string key);

    /// <summary>
    /// Limpa todo o cache
    /// </summary>
    void Clear();
}